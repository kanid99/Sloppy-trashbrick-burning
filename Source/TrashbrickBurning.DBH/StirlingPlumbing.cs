using DubsBadHygiene;
using RimWorld;
using Verse;

namespace TrashbrickBurning.DBH
{
    // Only built into Mods/DubsBadHygiene/<version>/Assemblies, which LoadFolders.xml loads only
    // when Dubs Bad Hygiene is active, so the main assembly never references BadHygiene.dll. The
    // engine's mode lives in the main assembly's CompStirlingEngine; this only does the plumbing.

    public class CompProperties_StirlingWater : CompProperties
    {
        /// <summary>Plumbing water drawn per day while water-cooled.</summary>
        public float waterPerDay = 50f;

        public CompProperties_StirlingWater()
        {
            compClass = typeof(CompStirlingWater);
        }
    }

    /// <summary>While the engine is water-cooled and burning, draws water from the plumbing and
    /// tells the engine whether it got any.</summary>
    public class CompStirlingWater : ThingComp
    {
        private const int Interval = GenTicks.TickRareInterval;

        private CompProperties_StirlingWater Props => (CompProperties_StirlingWater)props;

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

        private void Update()
        {
            CompStirlingEngine engine = parent.GetComp<CompStirlingEngine>();
            if (engine == null)
            {
                return;
            }
            bool flowing = false;
            if (engine.mode == StirlingMode.WaterCooled && engine.Burning)
            {
                PlumbingNet net = parent.GetComp<CompPipe>()?.pipeNet;
                if (net != null)
                {
                    flowing = net.PullWater(Props.waterPerDay * Interval / GenDate.TicksPerDay, out _);
                }
            }
            engine.waterFlowing = flowing;
            engine.Apply();
        }
    }

    /// <summary>
    /// DBH's boiler, feeding the plumbing's hot water and central heating, but only while the
    /// engine is in heat recovery mode. WorkingNow is replaced rather than extended: the base
    /// version checks the power comp's PowerOn, which means nothing on a generator.
    /// </summary>
    public class CompStirlingBoiler : CompBoiler
    {
        public override bool WorkingNow => parent.GetComp<CompStirlingEngine>()?.HeatRecoveryActive ?? false;
    }
}
