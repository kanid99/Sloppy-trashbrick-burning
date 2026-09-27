using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TrashbrickBurning
{
    /// <summary>What the Stirling engine's cold side is doing.</summary>
    public enum StirlingMode
    {
        /// <summary>Makes its own power. Always available.</summary>
        Power,

        /// <summary>Sends its heat down the pressurised hot water pipe to a steam turbine instead. Always available.</summary>
        TurbineFeed,

        /// <summary>Draws plumbing water to run cooler, for more power. Dubs Bad Hygiene only.</summary>
        WaterCooled,

        /// <summary>Works as a DBH boiler for hot water and heating, at reduced power. Dubs Bad Hygiene only.</summary>
        HeatRecovery
    }

    public class CompProperties_StirlingEngine : CompProperties
    {
        /// <summary>Heat sent to a turbine while feeding one, in the watts the turbine can make from it.</summary>
        public float turbineFeedWatts = 1400f;

        /// <summary>Output multiplier while water-cooled and actually getting water (DBH).</summary>
        public float cooledPowerFactor = 1.2f;

        /// <summary>Output multiplier in heat recovery; the rest of the heat goes to the boiler (DBH).</summary>
        public float heatRecoveryPowerFactor = 0.5f;

        public CompProperties_StirlingEngine()
        {
            compClass = typeof(CompStirlingEngine);
        }
    }

    /// <summary>
    /// Owns the engine's mode and sets <see cref="CompPowerPlantStirling.outputFactor"/> from it.
    /// The DBH modes only appear when Dubs Bad Hygiene is active; the DBH bridge assembly reports
    /// whether water is flowing through <see cref="waterFlowing"/> and runs the boiler off
    /// <see cref="HeatRecoveryActive"/>, so this assembly never references DBH.
    /// </summary>
    public class CompStirlingEngine : ThingComp
    {
        public const string DubsBadHygieneId = "Dubwise.DubsBadHygiene";

        public StirlingMode mode = StirlingMode.Power;

        /// <summary>Set by the DBH bridge each rare tick while water-cooled.</summary>
        public bool waterFlowing;

        public CompProperties_StirlingEngine Props => (CompProperties_StirlingEngine)props;

        private static bool DbhActive => ModsConfig.IsActive(DubsBadHygieneId);

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

        public bool FeedingTurbine => mode == StirlingMode.TurbineFeed && Burning;

        public bool HeatRecoveryActive => mode == StirlingMode.HeatRecovery && Burning;

        public IEnumerable<StirlingMode> AvailableModes()
        {
            yield return StirlingMode.Power;
            yield return StirlingMode.TurbineFeed;
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
            if (parent.IsHashIntervalTick(GenTicks.TickRareInterval))
            {
                Apply();
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            Apply();
        }

        public void Apply()
        {
            CompPowerPlantStirling plant = parent.GetComp<CompPowerPlantStirling>();
            if (plant == null)
            {
                return;
            }
            switch (mode)
            {
                case StirlingMode.TurbineFeed:
                    plant.outputFactor = 0f;
                    break;
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

        private void Cycle()
        {
            List<StirlingMode> modes = new List<StirlingMode>(AvailableModes());
            mode = modes[(modes.IndexOf(mode) + 1) % modes.Count];
            waterFlowing = false;
            Apply();
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
            string desc = "STB_ModeDesc".Translate(Props.turbineFeedWatts.ToString("0"));
            if (DbhActive)
            {
                desc += "\n" + "STB_ModeDescDBH".Translate(Props.cooledPowerFactor.ToStringPercent(),
                    Props.heatRecoveryPowerFactor.ToStringPercent());
            }
            yield return new Command_Action
            {
                defaultLabel = ("STB_Mode_" + mode).Translate(),
                defaultDesc = desc,
                icon = TexCommand.DesirePower,
                action = Cycle
            };
        }

        public override string CompInspectStringExtra()
        {
            string line = ("STB_Mode_" + mode).Translate();
            if (mode == StirlingMode.WaterCooled && !waterFlowing && Burning)
            {
                line += " (" + "STB_NoWater".Translate() + ")";
            }
            else if (mode == StirlingMode.TurbineFeed && !CompSteamTurbine.HasTurbine(parent))
            {
                line += " (" + "STB_NoTurbine".Translate() + ")";
            }
            return line;
        }
    }
}
