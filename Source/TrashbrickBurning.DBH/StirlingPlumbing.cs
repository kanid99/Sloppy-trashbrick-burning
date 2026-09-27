using System.Collections.Generic;
using DubsBadHygiene;
using RimWorld;
using Verse;

namespace TrashbrickBurning.DBH
{
    // Only built into Mods/DubsBadHygiene/<version>/Assemblies, which LoadFolders.xml loads only
    // when Dubs Bad Hygiene is active, so the main assembly never references BadHygiene.dll.

    public enum StirlingMode
    {
        Power,
        WaterCooled,
        HeatRecovery
    }

    public class CompProperties_StirlingPlumbing : CompProperties
    {
        /// <summary>Output multiplier while water-cooled and actually getting water.</summary>
        public float cooledPowerFactor = 1.2f;

        /// <summary>Plumbing water drawn per day while water-cooled.</summary>
        public float waterPerDay = 50f;

        /// <summary>Output multiplier in heat recovery: the rest of the heat goes to the boiler.</summary>
        public float heatRecoveryPowerFactor = 0.5f;

        public CompProperties_StirlingPlumbing()
        {
            compClass = typeof(CompStirlingPlumbing);
        }
    }

    /// <summary>
    /// The Stirling engine's cold side. Power: as without DBH. Water-cooled: draws plumbing
    /// water and runs hotter-to-colder, so it makes more power. Heat recovery: the waste heat
    /// goes into the plumbing's hot water and central heating instead (see CompStirlingBoiler),
    /// and less of it becomes electricity.
    /// </summary>
    public class CompStirlingPlumbing : ThingComp
    {
        private const int Interval = GenTicks.TickRareInterval;

        public StirlingMode mode = StirlingMode.Power;

        private bool gettingWater;

        private CompProperties_StirlingPlumbing Props => (CompProperties_StirlingPlumbing)props;

        private CompPowerPlantStirling Plant => parent.GetComp<CompPowerPlantStirling>();

        private CompPipe Pipe => parent.GetComp<CompPipe>();

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref mode, "stirlingMode", StirlingMode.Power);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Apply();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(Interval))
            {
                Update();
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            Update();
        }

        private bool Burning()
        {
            CompRefuelable fuel = parent.GetComp<CompRefuelable>();
            CompBreakdownable breakdown = parent.GetComp<CompBreakdownable>();
            return FlickUtility.WantsToBeOn(parent) && (fuel == null || fuel.HasFuel)
                   && (breakdown == null || !breakdown.BrokenDown);
        }

        private void Update()
        {
            gettingWater = false;
            if (mode == StirlingMode.WaterCooled && Burning())
            {
                PlumbingNet net = Pipe?.pipeNet;
                if (net != null)
                {
                    gettingWater = net.PullWater(Props.waterPerDay * Interval / GenDate.TicksPerDay, out _);
                }
            }
            Apply();
        }

        private void Apply()
        {
            CompPowerPlantStirling plant = Plant;
            if (plant == null)
            {
                return;
            }
            switch (mode)
            {
                case StirlingMode.WaterCooled:
                    plant.outputFactor = gettingWater ? Props.cooledPowerFactor : 1f;
                    break;
                case StirlingMode.HeatRecovery:
                    plant.outputFactor = Props.heatRecoveryPowerFactor;
                    break;
                default:
                    plant.outputFactor = 1f;
                    break;
            }
        }

        public bool HeatRecoveryActive => mode == StirlingMode.HeatRecovery && Burning();

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
            yield return new Command_Action
            {
                defaultLabel = ("STB_Mode_" + mode).Translate(),
                defaultDesc = "STB_ModeDesc".Translate(
                    Props.cooledPowerFactor.ToStringPercent(), Props.heatRecoveryPowerFactor.ToStringPercent()),
                icon = TexCommand.DesirePower,
                action = () =>
                {
                    mode = mode == StirlingMode.HeatRecovery ? StirlingMode.Power : mode + 1;
                    Update();
                }
            };
        }

        public override string CompInspectStringExtra()
        {
            string line = ("STB_Mode_" + mode).Translate();
            if (mode == StirlingMode.WaterCooled && !gettingWater && Burning())
            {
                line += " (" + "STB_NoWater".Translate() + ")";
            }
            return line;
        }
    }

    /// <summary>
    /// DBH's boiler, feeding the plumbing's hot water and central heating, but only while the
    /// engine is in heat recovery mode. WorkingNow is replaced rather than extended: the base
    /// version checks the power comp's PowerOn, which means nothing on a generator.
    /// </summary>
    public class CompStirlingBoiler : CompBoiler
    {
        public override bool WorkingNow => parent.GetComp<CompStirlingPlumbing>()?.HeatRecoveryActive ?? false;
    }
}
