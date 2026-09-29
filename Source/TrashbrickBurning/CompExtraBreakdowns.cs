using RimWorld;
using Verse;

namespace TrashbrickBurning
{
    public class CompProperties_ExtraBreakdowns : CompProperties
    {
        /// <summary>
        /// Mean days between the extra breakdowns, on top of vanilla's own CompBreakdownable roll.
        /// Vanilla's is 13.68 days, so the same again doubles the rate.
        /// </summary>
        public float mtbDays = 13.68f;

        public CompProperties_ExtraBreakdowns()
        {
            compClass = typeof(CompExtraBreakdowns);
        }
    }

    /// <summary>
    /// Cobbled gear breaks down more often: a second breakdown roll alongside vanilla's, on the same
    /// schedule and the same terms - only while it's switched on and not already broken.
    /// </summary>
    public class CompExtraBreakdowns : ThingComp
    {
        private const int CheckInterval = 1041;

        private CompProperties_ExtraBreakdowns Props => (CompProperties_ExtraBreakdowns)props;

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(CheckInterval))
            {
                Check(CheckInterval);
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            Check(GenTicks.TickRareInterval);
        }

        private void Check(int interval)
        {
            CompBreakdownable breakdown = parent.GetComp<CompBreakdownable>();
            if (breakdown == null || breakdown.BrokenDown || !parent.Spawned || !FlickUtility.WantsToBeOn(parent))
            {
                return;
            }
            if (Rand.MTBEventOccurs(Props.mtbDays, GenDate.TicksPerDay, interval))
            {
                breakdown.DoBreakdown();
            }
        }
    }
}
