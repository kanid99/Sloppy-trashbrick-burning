using System.Collections.Generic;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    public class CompProperties_SteamTurbine : CompProperties_Power
    {
        /// <summary>The most this turbine can make, however much heat is piped to it.</summary>
        public float capacityWatts = 5000f;

        public CompProperties_SteamTurbine()
        {
            compClass = typeof(CompSteamTurbine);
        }
    }

    /// <summary>
    /// A generator driven by the pressurised hot water network. Vanilla Expanded Framework's
    /// PipeSystem carries the pipes, network, overlay and valves; this only does the sums. VEF's own
    /// resource traders switch a consumer fully on or fully off, which would leave a big turbine idle
    /// on one stove, so instead each turbine adds up the stoves on its network that are feeding
    /// turbines, and takes its share by capacity.
    /// </summary>
    public class CompSteamTurbine : CompPowerPlant
    {
        private const int RecalcInterval = 60;

        private float watts;

        public float SuppliedWatts { get; private set; }

        public new CompProperties_SteamTurbine Props => (CompProperties_SteamTurbine)props;

        protected override float DesiredPowerOutput => watts;

        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(RecalcInterval))
            {
                Recalculate();
            }
            base.CompTick();
        }

        private static PipeNet NetOf(ThingWithComps thing)
        {
            List<ThingComp> comps = thing.AllComps;
            for (int i = 0; i < comps.Count; i++)
            {
                if (comps[i] is CompResource res && res.PipeNet != null)
                {
                    return res.PipeNet;
                }
            }
            return null;
        }

        private void Recalculate()
        {
            watts = 0f;
            SuppliedWatts = 0f;
            PipeNet net = NetOf(parent);
            if (net == null)
            {
                return;
            }
            float supply = 0f, capacity = 0f;
            HashSet<Thing> seen = new HashSet<Thing>();
            foreach (CompResource connector in net.connectors)
            {
                ThingWithComps thing = connector.parent;
                if (!seen.Add(thing))
                {
                    continue;
                }
                CompStirlingEngine engine = thing.GetComp<CompStirlingEngine>();
                if (engine != null && engine.FeedingTurbine)
                {
                    supply += engine.Props.turbineFeedWatts;
                }
                CompSteamTurbine turbine = thing.GetComp<CompSteamTurbine>();
                if (turbine != null && turbine.CanRun)
                {
                    capacity += turbine.Props.capacityWatts;
                }
            }
            if (capacity <= 0f || !CanRun)
            {
                return;
            }
            // Share the network's heat by capacity, capped at this turbine's own.
            SuppliedWatts = supply * Props.capacityWatts / capacity;
            watts = Mathf.Min(SuppliedWatts, Props.capacityWatts);
        }

        private bool CanRun
        {
            get
            {
                CompBreakdownable breakdown = parent.GetComp<CompBreakdownable>();
                return FlickUtility.WantsToBeOn(parent) && (breakdown == null || !breakdown.BrokenDown);
            }
        }

        /// <summary>True if a turbine shares a hot water network with this building.</summary>
        public static bool HasTurbine(ThingWithComps thing)
        {
            PipeNet net = NetOf(thing);
            if (net == null)
            {
                return false;
            }
            foreach (CompResource connector in net.connectors)
            {
                if (connector.parent.GetComp<CompSteamTurbine>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        public override string CompInspectStringExtra()
        {
            string baseString = base.CompInspectStringExtra();
            string line = "STB_TurbineLoad".Translate(SuppliedWatts.ToString("0"), Props.capacityWatts.ToString("0"));
            return baseString.NullOrEmpty() ? line : baseString + "\n" + line;
        }
    }
}
