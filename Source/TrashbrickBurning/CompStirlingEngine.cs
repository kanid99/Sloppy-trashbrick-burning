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
    /// Advanced play mode: one operating mode - eco, normal or high. Eco gets the most heat from each
    /// brick but makes the least; high makes the most and wastes the most.
    /// </summary>
    public class HeatLevel
    {
        /// <summary>"Eco", "Normal" or "High": the STB_BurnMode_ key.</summary>
        public string key = "Normal";
        public float watts;
        public float fuelPerDay;
        public float roomHeatPerSecond;

        /// <summary>
        /// The built-in Stirling engine's output in this mode. It uses three times this in heat, and
        /// runs only while no steam turbine is on the burner's network. 0 for burners without one.
        /// </summary>
        public float stirlingWatts;

        /// <summary>DBH plumbing water drawn a day in this mode, while burning on plumbing.</summary>
        public float waterPerDay;
    }

    /// <summary>Fuel units one item of another fuel is worth. A trashbrick is 1.</summary>
    public class FuelValue
    {
        public ThingDef thing;
        public float value = 1f;
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

        /// <summary>
        /// Advanced: true for a burner with a working safety valve, which vents steam harmlessly at
        /// high pressure. The cobbled stove has none - it bursts.
        /// </summary>
        public bool safetyValve;

        /// <summary>Ash made per unit of fuel burnt.</summary>
        public float ashPerFuel = 0.2f;

        /// <summary>Other fuels the burner takes, and what each is worth. Only while the setting allows them.</summary>
        public List<FuelValue> otherFuels = new List<FuelValue>();

        public CompProperties_StirlingEngine()
        {
            compClass = typeof(CompStirlingEngine);
        }

        public float FuelValueOf(ThingDef def)
        {
            if (otherFuels != null)
            {
                for (int i = 0; i < otherFuels.Count; i++)
                {
                    if (otherFuels[i].thing == def)
                    {
                        return otherFuels[i].value;
                    }
                }
            }
            return 1f;
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
    /// burn rate can differ per stove), pushes the waste heat into the room, fills the ash pan, and
    /// sets the power plant's output.
    ///
    /// In advanced play mode it's a burner with three modes (eco, normal, high). With DBH its hot
    /// water share goes to DBH first. With no steam turbine on its network, its own Stirling engine
    /// takes three times its output in heat. The rest goes, in order, to turbines, radiators and
    /// Overpressure Tanks on its pressurised hot water network (HeatNetwork), then to DBH hot water.
    /// Whatever is still left builds pressure: a burner with a safety valve vents it, one without
    /// eventually bursts.
    /// </summary>
    public class CompStirlingEngine : ThingComp
    {
        public const string DubsBadHygieneId = "Dubwise.DubsBadHygiene";
        private const int Interval = 60;

        /// <summary>Pressure climbs from empty to bursting in half a day at 500W of unused heat.</summary>
        private const float PressureWattsPerHalfDay = 500f;

        /// <summary>The Stirling engine turns a third of the heat it takes into power.</summary>
        public const float StirlingHeatPerWatt = 3f;

        private const float VentAt = 0.85f;
        public const float WarnAt = 0.7f;

        public StirlingMode mode = StirlingMode.Power;
        public int heatLevel;

        /// <summary>Simple mode, set by the DBH bridge: water is flowing while water-cooled.</summary>
        public bool waterFlowing;

        /// <summary>Advanced mode, set by the DBH bridge: heat the hot water system actually drew, in watts.</summary>
        public float hotWaterDrawWatts;

        /// <summary>Advanced mode, set by the DBH bridge: the part of that draw that came out of the
        /// hot water share, i.e. was taken before the network got anything.</summary>
        public float hotWaterDrawReservedWatts;

        /// <summary>
        /// Advanced mode with DBH: the share of this burner's heat offered to DBH's hot water and
        /// heating FIRST, before the turbines. 0 to 1. Whatever DBH doesn't actually draw of it goes
        /// on to the network, so a high share on a satisfied tank costs the turbines nothing.
        /// </summary>
        public float hotWaterShare;

        /// <summary>Advanced mode, from HeatNetwork: heat taken by the network, and what's left.</summary>
        public float toNetworkWatts;
        public float surplusWatts;
        public bool networkConnected;

        /// <summary>0 to 1; at 1 a burner without a safety valve bursts.</summary>
        public float pressure;

        private float ashBuffer;
        private bool venting;

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

        public float FuelPerDay =>
            (Advanced ? Level.fuelPerDay : Props.simpleFuelPerDay) * TrashbrickBurningMod.S.fuelUseMultiplier;

        public float RoomHeatPerSecond => Advanced ? Level.roomHeatPerSecond : Props.simpleRoomHeatPerSecond;

        /// <summary>Advanced mode with DBH: the heat offered to hot water first, before the network.</summary>
        public float ReservedWatts => DbhActive ? HeatWatts * Mathf.Clamp01(hotWaterShare) : 0f;

        /// <summary>
        /// The Stirling engine runs only while no steam turbine is on the burner's network - piped to
        /// nothing, or to radiators and tanks alone.
        /// </summary>
        public bool StirlingActive =>
            Advanced && Burning && HasEngine && Level.stirlingWatts > 0f && !HeatNetwork.HasTurbine(HeatNetwork.NetOf(parent));

        /// <summary>Heat the Stirling engine takes: three times its output, from what hot water left.</summary>
        public float StirlingHeatWatts =>
            StirlingActive ? Mathf.Min(Mathf.Max(0f, HeatWatts - hotWaterDrawReservedWatts), Level.stirlingWatts * StirlingHeatPerWatt) : 0f;

        public float StirlingPowerWatts => StirlingHeatWatts / StirlingHeatPerWatt;

        /// <summary>The heat the pressurised hot water network gets: what hot water and the Stirling engine left.</summary>
        public float NetworkHeatWatts => Mathf.Max(0f, HeatWatts - hotWaterDrawReservedWatts - StirlingHeatWatts);

        /// <summary>
        /// Heat nothing took - not the network, not hot water - which builds pressure. A burner piped
        /// to nothing still has its Stirling engine's leftover heat: it needs an Overpressure Tank too.
        /// </summary>
        public float UnusedWatts => Mathf.Max(0f, surplusWatts - (hotWaterDrawWatts - hotWaterDrawReservedWatts));

        /// <summary>Simple mode, read by the DBH boiler.</summary>
        public bool HeatRecoveryActive => !Advanced && mode == StirlingMode.HeatRecovery && Burning;

        public bool Venting => venting;

        public float AshBuffer => ashBuffer;

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
            Scribe_Values.Look(ref pressure, "pressure", 0f);
            Scribe_Values.Look(ref ashBuffer, "ashBuffer", 0f);
            Scribe_Values.Look(ref hotWaterShare, "hotWaterShare", 0f);
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
                float burnt = FuelPerDay * ticks / GenDate.TicksPerDay;
                parent.GetComp<CompRefuelable>()?.ConsumeFuel(burnt);
                ashBuffer += burnt * Props.ashPerFuel * TrashbrickBurningMod.S.ashMultiplier;
                if (parent.Spawned)
                {
                    // CompHeatPusher's rate is per second, pushed once every 60 ticks.
                    GenTemperature.PushHeat(parent, RoomHeatPerSecond * ticks / 60f);
                }
            }
            if (parent.Spawned)
            {
                Ash.DropWholeUnits(this, ref ashBuffer);
            }
            if (Advanced)
            {
                HeatNetwork.UpdateBurner(this);
                UpdatePressure(ticks);
            }
            else
            {
                pressure = 0f;
                venting = false;
            }
            Apply();
        }

        private void UpdatePressure(int ticks)
        {
            float days = (float)ticks / GenDate.TicksPerDay;
            float unused = UnusedWatts;
            if (unused > 1f)
            {
                // Scaled to the burner's top rate, so a big burner takes as long to fill as a small one.
                float scale = Mathf.Max(1f, Props.heatLevels.Count > 0 ? Props.heatLevels[Props.heatLevels.Count - 1].watts / 500f : 1f);
                pressure += unused / (PressureWattsPerHalfDay * scale) * days * 2f;
            }
            else
            {
                pressure = Mathf.Max(0f, pressure - days * 2f);
            }
            venting = false;
            if (pressure < VentAt || !parent.Spawned)
            {
                return;
            }
            // With hazards off, every burner behaves as if it had a safety valve.
            if (Props.safetyValve || !TrashbrickBurningMod.Hazards)
            {
                pressure = VentAt;
                venting = true;
                return;
            }
            if (pressure >= 1f)
            {
                pressure = 0f;
                SteamBurst.Burst(parent);
            }
        }

        /// <summary>
        /// The small burners carry a Stirling engine; the large and industrial ones are all heat and
        /// no engine, and make nothing without a steam turbine.
        /// </summary>
        public bool HasEngine => parent.GetComp<CompPowerPlantStirling>() != null;

        public void Apply()
        {
            CompPowerPlantStirling plant = parent.GetComp<CompPowerPlantStirling>();
            if (plant == null)
            {
                return;
            }
            float power = TrashbrickBurningMod.S.powerMultiplier;
            if (Advanced)
            {
                plant.outputFactor = 1f;
                plant.fixedWatts = StirlingPowerWatts * power;
                return;
            }
            plant.fixedWatts = null;
            switch (mode)
            {
                case StirlingMode.WaterCooled:
                    plant.outputFactor = (waterFlowing ? Props.cooledPowerFactor : 1f) * power;
                    break;
                case StirlingMode.HeatRecovery:
                    plant.outputFactor = Props.heatRecoveryPowerFactor * power;
                    break;
                default:
                    plant.outputFactor = power;
                    break;
            }
        }

        public void DumpAsh()
        {
            Ash.DropAll(this, ref ashBuffer);
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
            if (ashBuffer > 0.05f)
            {
                yield return new Command_Action
                {
                    defaultLabel = "STB_DumpAsh".Translate(),
                    defaultDesc = "STB_DumpAshDesc".Translate(),
                    icon = TexCommand.Install,
                    action = DumpAsh
                };
            }
            if (Advanced && DbhActive)
            {
                yield return new Command_HotWaterShare(this);
            }
            if (Advanced)
            {
                yield return new Command_SyncBurners(this);
            }
            if (Advanced)
            {
                yield return new Command_Action
                {
                    defaultLabel = "STB_BurnRate".Translate(("STB_BurnMode_" + Level.key).Translate(), Level.watts.ToString("0")),
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
            float fuelMult = TrashbrickBurningMod.S.fuelUseMultiplier;
            foreach (HeatLevel level in Props.heatLevels)
            {
                float fuel = level.fuelPerDay * fuelMult;
                table += "\n" + (HasEngine ? "STB_BurnRateLine" : "STB_BurnRateLineNoEngine").Translate(
                    ("STB_BurnMode_" + level.key).Translate(), level.watts.ToString("0"), fuel.ToString("0.#"),
                    (level.watts / Mathf.Max(0.01f, fuel)).ToString("0.0"),
                    (level.stirlingWatts * TrashbrickBurningMod.S.powerMultiplier).ToString("0"),
                    (level.watts - level.stirlingWatts * StirlingHeatPerWatt).ToString("0"));
            }
            return table;
        }

        public override string CompInspectStringExtra()
        {
            List<string> lines = new List<string>();
            if (Advanced)
            {
                lines.Add("STB_BurnerStatus".Translate(HeatWatts.ToString("0"), FuelPerDay.ToString("0.#")));
                if (Burning)
                {
                    if (StirlingActive)
                    {
                        lines.Add("STB_StirlingStatus".Translate(
                            (StirlingPowerWatts * TrashbrickBurningMod.S.powerMultiplier).ToString("0"),
                            StirlingHeatWatts.ToString("0")));
                    }
                    else if (HasEngine && Level.stirlingWatts > 0f)
                    {
                        lines.Add("STB_StirlingOffTurbine".Translate());
                    }
                    if (networkConnected)
                    {
                        lines.Add("STB_ToNetwork".Translate(toNetworkWatts.ToString("0")));
                    }
                    else if (!HasEngine)
                    {
                        lines.Add("STB_NoNetworkNoEngine".Translate());
                    }
                    if (UnusedWatts > 1f)
                    {
                        lines.Add("STB_UnusedHeat".Translate(UnusedWatts.ToString("0")));
                    }
                    if (DbhActive)
                    {
                        lines.Add("STB_HotWaterShareStatus".Translate(hotWaterShare.ToStringPercent(),
                            ReservedWatts.ToString("0"), hotWaterDrawWatts.ToString("0")));
                    }
                }
                if (pressure > 0.01f)
                {
                    string p = "STB_Pressure".Translate(pressure.ToStringPercent());
                    if (venting)
                    {
                        p += " " + "STB_Venting".Translate();
                    }
                    else if (pressure >= WarnAt)
                    {
                        p += " " + (Props.safetyValve ? "STB_NearVent" : "STB_NearBurst").Translate();
                    }
                    lines.Add(p);
                }
            }
            else
            {
                lines.Add("STB_SimpleStatus".Translate(FuelPerDay.ToString("0.#")));
                if (DbhActive)
                {
                    string line = ("STB_Mode_" + mode).Translate();
                    if (mode == StirlingMode.WaterCooled && !waterFlowing && Burning)
                    {
                        line += " (" + "STB_NoWater".Translate() + ")";
                    }
                    lines.Add(line);
                }
            }
            if (ashBuffer > 0.05f)
            {
                lines.Add("STB_AshPan".Translate(ashBuffer.ToString("0.0")));
            }
            return string.Join("\n", lines);
        }
    }

    /// <summary>
    /// The hot water share: left-click steps it up by 10% (wrapping to 0), right-click picks a value.
    /// Takes the slot DBH's electric boiler "power mode" stepper would have, which can't go to zero
    /// and is wired to that boiler's electricity use, so it's hidden on burners.
    /// </summary>
    public class Command_HotWaterShare : Command_Action
    {
        private readonly CompStirlingEngine engine;

        public Command_HotWaterShare(CompStirlingEngine engine)
        {
            this.engine = engine;
            defaultLabel = "STB_HotWaterShare".Translate(engine.hotWaterShare.ToStringPercent());
            defaultDesc = "STB_HotWaterShareDesc".Translate();
            icon = TexCommand.DesirePower;
            action = () => Set(Mathf.Round(engine.hotWaterShare * 10f + 1f) % 11f / 10f);
        }

        private void Set(float share)
        {
            engine.hotWaterShare = Mathf.Clamp01(share);
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                for (int i = 0; i <= 10; i++)
                {
                    float share = i / 10f;
                    yield return new FloatMenuOption(share.ToStringPercent(), () => Set(share));
                }
            }
        }
    }

    /// <summary>
    /// Copies this burner's burn rate and hot water share to other burners, so a row of them doesn't
    /// need setting one by one. Left-click: every burner on this burner's pressurised hot water
    /// network, or every burner on the map if it isn't piped to anything. Right-click: pick which.
    /// Burners are matched by mode (eco, normal, high), so cobbled and proper burners sync together.
    /// </summary>
    public class Command_SyncBurners : Command_Action
    {
        private readonly CompStirlingEngine source;

        public Command_SyncBurners(CompStirlingEngine source)
        {
            this.source = source;
            defaultLabel = "STB_SyncBurners".Translate();
            defaultDesc = "STB_SyncBurnersDesc".Translate();
            icon = ContentFinder<Texture2D>.Get("UI/Commands/CopySettings", false) ?? TexCommand.ForbidOff;
            action = () =>
            {
                PipeSystem.PipeNet net = HeatNetwork.NetOf(source.parent);
                Sync(net != null ? Network(net) : OnMap());
            };
        }

        private IEnumerable<CompStirlingEngine> OnMap()
        {
            Map map = source.parent.Map;
            if (map == null)
            {
                yield break;
            }
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                CompStirlingEngine e = b.GetComp<CompStirlingEngine>();
                if (e != null)
                {
                    yield return e;
                }
            }
        }

        private static IEnumerable<CompStirlingEngine> Network(PipeSystem.PipeNet net)
        {
            foreach (ThingWithComps thing in HeatNetwork.Members(net))
            {
                CompStirlingEngine e = thing.GetComp<CompStirlingEngine>();
                if (e != null)
                {
                    yield return e;
                }
            }
        }

        private void Sync(IEnumerable<CompStirlingEngine> targets)
        {
            int count = 0;
            foreach (CompStirlingEngine e in targets)
            {
                if (e == source)
                {
                    continue;
                }
                // Match by mode (eco, normal, high): the cobbled and proper burners differ in watts.
                int best = e.Props.heatLevels.FindIndex(l => l.key == source.Level.key);
                e.heatLevel = best >= 0 ? best : Mathf.Clamp(source.heatLevel, 0, e.Props.heatLevels.Count - 1);
                e.hotWaterShare = source.hotWaterShare;
                e.Apply();
                count++;
            }
            Messages.Message("STB_SyncedBurners".Translate(count), source.parent, MessageTypeDefOf.NeutralEvent, false);
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                PipeSystem.PipeNet net = HeatNetwork.NetOf(source.parent);
                if (net != null)
                {
                    yield return new FloatMenuOption("STB_SyncNetwork".Translate(), () => Sync(Network(net)));
                }
                yield return new FloatMenuOption("STB_SyncMap".Translate(), () => Sync(OnMap()));
            }
        }
    }
}
