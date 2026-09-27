using RimWorld;
using Verse;

namespace TrashbrickBurning
{
    public class CompProperties_BurnPollution : CompProperties
    {
        /// <summary>Terrain cells polluted per day of continuous burning.</summary>
        public float cellsPerDay = 3f;

        public CompProperties_BurnPollution()
        {
            compClass = typeof(CompBurnPollution);
        }
    }

    /// <summary>
    /// Pollutes the ground around the parent, but only while it's actually burning: switched on,
    /// fuelled and not broken down. Vanilla's CompPolluteOverTime has no such check. No-op without Biotech.
    /// </summary>
    public class CompBurnPollution : ThingComp
    {
        private float progress;

        private CompProperties_BurnPollution Props => (CompProperties_BurnPollution)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref progress, "pollutionProgress", 0f);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(GenTicks.TickRareInterval))
            {
                Accumulate(GenTicks.TickRareInterval);
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            Accumulate(GenTicks.TickRareInterval);
        }

        private void Accumulate(int ticks)
        {
            if (!ModsConfig.BiotechActive || !parent.Spawned || !IsBurning())
            {
                return;
            }
            progress += Props.cellsPerDay * ticks / GenDate.TicksPerDay;
            if (progress >= 1f)
            {
                int cells = (int)progress;
                progress -= cells;
                PollutionUtility.GrowPollutionAt(parent.Position, parent.Map, cells);
            }
        }

        private bool IsBurning()
        {
            CompRefuelable fuel = parent.GetComp<CompRefuelable>();
            if (fuel != null && !fuel.HasFuel)
            {
                return false;
            }
            CompFlickable flick = parent.GetComp<CompFlickable>();
            if (flick != null && !flick.SwitchIsOn)
            {
                return false;
            }
            CompBreakdownable breakdown = parent.GetComp<CompBreakdownable>();
            return breakdown == null || !breakdown.BrokenDown;
        }

        public override string CompInspectStringExtra()
        {
            return ModsConfig.BiotechActive
                ? "STB_PollutesWhileBurning".Translate(Props.cellsPerDay.ToString("0.#"))
                : null;
        }
    }
}
