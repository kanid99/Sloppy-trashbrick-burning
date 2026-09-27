using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    /// <summary>Simple play mode: what the stove's own Stirling engine does with its heat.</summary>
    public enum StirlingMode
    {
        /// <summary>Makes its own power.</summary>
        Power,

        /// <summary>Draws plumbing water to run cooler, for more power. Dubs Bad Hygiene only.</summary>
        WaterCooled,

        /// <summary>Works as a DBH boiler for hot water and heating, at reduced power. Dubs Bad Hygiene only.</summary>
        HeatRecovery
    }

    /// <summary>
    /// Advanced play mode: one burn rate. Hotter burns cost more fuel per watt and waste more heat
    /// into the room, but give the built-in engine more to work with when no turbine is connected.
    /// </summary>
    public class HeatLevel
    {
        public float watts;
        public float fuelPerDay;
        public float roomHeatPerSecond;

        /// <summary>The built-in engine's output at this rate, with no turbine connected.</summary>
        public float builtInWatts = 100f;
    }

    public class CompProperties_StirlingEngine : CompProperties
    {
        // Simple play mode: a self-contained generator, as the def's power comp describes.
        public float simpleFuelPerDay = 20f;
        public float simpleRoomHeatPerSecond = 8f;
        public float cooledPowerFactor = 1.2f;
        public float heatRecoveryPowerFactor = 0.5f;

        // Advanced play mode: a burner.
        public List<HeatLevel> heatLevels = new List<HeatLevel>();

        public CompProperties_StirlingEngine()
        {
            compClass = typeof(CompStirlingEngine);
        }

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (heatLevels.NullOrEmpty())
            {
                yield return "CompProperties_StirlingEngine has no heatLevels";
            }
            CompProperties_Refuelable fuel = parentDef.GetCompProperties<CompProperties_Refuelable>();
            if (fuel != null && !fuel.externalTicking)
            {
                yield return "CompProperties_StirlingEngine burns the fuel itself; set externalTicking on CompProperties_Refuelable";
            }
        }
    }

    /// <summary>
    /// The stove's firebox and engine. Burns the fuel (CompRefuelable is externally ticked, so the
    /// burn rate can differ per stove), pushes the waste heat into the room, and sets the power
    /// plant's output. In advanced play mode it's a burner whose heat goes, in order, to steam
    /// turbines on its pressurised hot water network, then to Dubs Bad Hygiene hot water if the
    /// bridge assembly is loaded, and only with no turbine connected to its own small engine
    /// (100-300W, by burn rate).
    /// </summary>
    public class CompStirlingEngine : ThingComp
    {
        public const string DubsBadHygieneId = "Dubwise.DubsBadHygiene";
        private const int Interval = 60;

        public StirlingMode mode = StirlingMode.Power;
        public int heatLevel;

        /// <summary>Simple mode, set by the DBH bridge: water is flowing while water-cooled.</summary>
        public bool waterFlowing;

        /// <summary>Advanced mode, set by the DBH bridge: heat the hot water system actually drew, in watts.</summary>
        public float hotWaterDrawWatts;

        /// <summary>Advanced mode, from HotWaterAllocation: heat taken by turbines, and what's left.</summary>
        public float toTurbinesWatts;
        public float surplusWatts;
        public bool turbineConnected;

        public CompProperties_StirlingEngine Props => (CompProperties_StirlingEngine)props;

        public static bool Advanced => TrashbrickBurningMod.Advanced;

        private static bool DbhActive => ModsConfig.IsActive(DubsBadHygieneId);

        public HeatLevel Level => Props.heatLevels[Mathf.Clamp(heatLevel, 0, Props.heatLevels.Count - 1)];

        public bool Burning
        {
            get
            {
                CompRefuelable fuel = parent.GetComp<CompRefuelable>();
                CompBreakdownable breakdown = parent.GetComp<CompBreakdownable>();
                return FlickUtility.WantsToBeOn(parent) && (fuel == null || fuel.HasFuel)
                       && (breakdown == null || !breakdown.BrokenDown);
            }
        }

        /// <summary>Advanced mode: the heat this burner is making right now.</summary>
        public float HeatWatts => Advanced && Burning ? Level.watts : 0f;

        public float FuelPerDay => Advanced ? Level.fuelPerDay : Props.simpleFuelPerDay;

        public float RoomHeatPerSecond => Advanced ? Level.roomHeatPerSecond : Props.simpleRoomHeatPerSecond;

        /// <summary>Simple mode, read by the DBH boiler.</summary>
        public bool HeatRecoveryActive => !Advanced && mode == StirlingMode.HeatRecovery && Burning;

        public IEnumerable<StirlingMode> AvailableModes()
        {
            yield return StirlingMode.Power;
            if (DbhActive)
            {
                yield return StirlingMode.WaterCooled;
                yield return StirlingMode.HeatRecovery;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref mode, "stirlingMode", StirlingMode.Power);
            Scribe_Values.Look(ref heatLevel, "heatLevel", 0);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            // A save made with DBH, loaded without it, falls back to plain power.
            if (!new List<StirlingMode>(AvailableModes()).Contains(mode))
            {
                mode = StirlingMode.Power;
            }
            Apply();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(Interval))
            {
                Step(Interval);
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            Step(GenTicks.TickRareInterval);
        }

        private void Step(int ticks)
        {
            if (Burning)
            {
                parent.GetComp<CompRefuelable>()?.ConsumeFuel(FuelPerDay * ticks / GenDate.TicksPerDay);
                if (parent.Spawned)
                {
                    // CompHeatPusher's rate is per second, pushed once every 60 ticks.
                    GenTemperature.PushHeat(parent, RoomHeatPerSecond * ticks / 60f);
                }
            }
            if (Advanced)
            {
                HotWaterAllocation.UpdateBurner(this);
            }
            Apply();
        }

        public void Apply()
        {
            CompPowerPlantStirling plant = parent.GetComp<CompPowerPlantStirling>();
            if (plant == null)
            {
                return;
            }
            if (Advanced)
            {
                plant.outputFactor = 1f;
                if (turbineConnected || !Burning)
                {
                    plant.fixedWatts = 0f;
                }
                else
                {
                    // Heat the hot water system draws doesn't turn the engine: its power drops
                    // in proportion, down to nothing.
                    float heat = Mathf.Max(1f, HeatWatts);
                    plant.fixedWatts = Level.builtInWatts * Mathf.Clamp01(1f - hotWaterDrawWatts / heat);
                }
                return;
            }
            plant.fixedWatts = null;
            switch (mode)
            {
                case StirlingMode.WaterCooled:
                    plant.outputFactor = waterFlowing ? Props.cooledPowerFactor : 1f;
                    break;
                case StirlingMode.HeatRecovery:
                    plant.outputFactor = Props.heatRecoveryPowerFactor;
                    break;
                default:
                    plant.outputFactor = 1f;
                    break;
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (parent.Faction != Faction.OfPlayer)
            {
                yield break;
            }
            if (Advanced)
            {
                yield return new Command_Action
                {
                    defaultLabel = "STB_BurnRate".Translate(Level.watts.ToString("0")),
                    defaultDesc = "STB_BurnRateDesc".Translate(BurnRateTable()),
                    icon = TexCommand.DesirePower,
                    action = () =>
                    {
                        heatLevel = (heatLevel + 1) % Props.heatLevels.Count;
                        Step(0);
                    }
                };
                yield break;
            }
            if (!DbhActive)
            {
                yield break;
            }
            yield return new Command_Action
            {
                defaultLabel = ("STB_Mode_" + mode).Translate(),
                defaultDesc = "STB_ModeDescDBH".Translate(Props.cooledPowerFactor.ToStringPercent(),
                    Props.heatRecoveryPowerFactor.ToStringPercent()),
                icon = TexCommand.DesirePower,
                action = () =>
                {
                    List<StirlingMode> modes = new List<StirlingMode>(AvailableModes());
                    mode = modes[(modes.IndexOf(mode) + 1) % modes.Count];
                    waterFlowing = false;
                    Apply();
                }
            };
        }

        private string BurnRateTable()
        {
            string table = "";
            foreach (HeatLevel level in Props.heatLevels)
            {
                table += "\n" + "STB_BurnRateLine".Translate(level.watts.ToString("0"), level.fuelPerDay.ToString("0.#"),
                    (level.fuelPerDay * 1000f / level.watts).ToString("0.0"), level.roomHeatPerSecond.ToString("0.#"),
                    level.builtInWatts.ToString("0"));
            }
            return table;
        }

        public override string CompInspectStringExtra()
        {
            if (Advanced)
            {
                string s = "STB_BurnerStatus".Translate(HeatWatts.ToString("0"), FuelPerDay.ToString("0.#"));
                if (!Burning)
                {
                    return s;
                }
                if (turbineConnected)
                {
                    s += "\n" + "STB_ToTurbines".Translate(toTurbinesWatts.ToString("0"));
                }
                else
                {
                    s += "\n" + "STB_NoTurbineBuiltIn".Translate(Level.builtInWatts.ToString("0"));
                }
                if (hotWaterDrawWatts > 0.5f)
                {
                    s += "\n" + "STB_ToHotWater".Translate(hotWaterDrawWatts.ToString("0"));
                }
                return s;
            }
            string line = "STB_SimpleStatus".Translate(FuelPerDay.ToString("0.#"));
            if (DbhActive)
            {
                line += "\n" + ("STB_Mode_" + mode).Translate();
                if (mode == StirlingMode.WaterCooled && !waterFlowing && Burning)
                {
                    line += " (" + "STB_NoWater".Translate() + ")";
                }
            }
            return line;
        }
    }
}
