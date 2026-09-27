using System.Collections.Generic;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    public class CompProperties_SteamTurbine : CompProperties_Power
    {
        /// <summary>The most heat this turbine can take.</summary>
        public float capacityWatts = 1500f;

        /// <summary>Power made per watt of heat taken. The cobbled turbine wastes a quarter of it.</summary>
        public float efficiency = 1f;

        public CompProperties_SteamTurbine()
        {
            compClass = typeof(CompSteamTurbine);
        }
    }

    /// <summary>
    /// Shares the heat on one pressurised hot water network between its turbines, turbines first:
    /// they take up to their combined capacity, and each burner's leftover is what DBH's hot water
    /// may take. Worked out once per network per tick and read by every burner and turbine on it.
    /// Vanilla Expanded Framework's PipeSystem carries the pipes and networks; its own resource
    /// traders switch a consumer fully on or off, which would idle a turbine short of heat, so the
    /// sums are done here instead.
    /// </summary>
    public static class HotWaterAllocation
    {
        private struct Result
        {
            public float heat, capacity, used;
            public bool anyTurbine;
        }

        private static readonly Dictionary<PipeNet, Result> Cache = new Dictionary<PipeNet, Result>();
        private static int cacheTick = -1;

        public static PipeNet NetOf(ThingWithComps thing)
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

        private static Result Compute(PipeNet net)
        {
            int tick = Find.TickManager.TicksGame;
            if (tick != cacheTick)
            {
                Cache.Clear();
                cacheTick = tick;
            }
            if (Cache.TryGetValue(net, out Result cached))
            {
                return cached;
            }
            Result r = new Result();
            HashSet<Thing> seen = new HashSet<Thing>();
            foreach (CompResource connector in net.connectors)
            {
                ThingWithComps thing = connector.parent;
                if (!seen.Add(thing))
                {
                    continue;
                }
                CompStirlingEngine engine = thing.GetComp<CompStirlingEngine>();
                if (engine != null)
                {
                    r.heat += engine.HeatWatts;
                }
                CompSteamTurbine turbine = thing.GetComp<CompSteamTurbine>();
                if (turbine != null)
                {
                    r.anyTurbine = true;
                    if (turbine.CanRun)
                    {
                        r.capacity += turbine.Props.capacityWatts;
                    }
                }
            }
            r.used = Mathf.Min(r.heat, r.capacity);
            Cache[net] = r;
            return r;
        }

        public static void UpdateBurner(CompStirlingEngine engine)
        {
            float heat = engine.HeatWatts;
            PipeNet net = NetOf(engine.parent);
            if (net == null)
            {
                engine.turbineConnected = false;
                engine.toTurbinesWatts = 0f;
                engine.surplusWatts = heat;
                return;
            }
            Result r = Compute(net);
            engine.turbineConnected = r.anyTurbine;
            float share = r.heat > 0f ? r.used / r.heat : 0f;
            engine.toTurbinesWatts = heat * share;
            engine.surplusWatts = heat - engine.toTurbinesWatts;
        }

        public static float TurbineOutput(CompSteamTurbine turbine)
        {
            PipeNet net = NetOf(turbine.parent);
            if (net == null || !turbine.CanRun)
            {
                return 0f;
            }
            Result r = Compute(net);
            float heat = r.capacity > 0f ? r.used * turbine.Props.capacityWatts / r.capacity : 0f;
            return heat * turbine.Props.efficiency;
        }
    }

    /// <summary>A generator that turns the heat piped to it into power at its efficiency, up to its capacity.</summary>
    public class CompSteamTurbine : CompPowerPlant
    {
        private const int RecalcInterval = 60;

        private float watts;

        private float heatWatts;

        public new CompProperties_SteamTurbine Props => (CompProperties_SteamTurbine)props;

        protected override float DesiredPowerOutput => watts;

        public bool CanRun
        {
            get
            {
                CompBreakdownable breakdown = parent.GetComp<CompBreakdownable>();
                return FlickUtility.WantsToBeOn(parent) && (breakdown == null || !breakdown.BrokenDown);
            }
        }

        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(RecalcInterval))
            {
                watts = TrashbrickBurningMod.Advanced ? HotWaterAllocation.TurbineOutput(this) : 0f;
                heatWatts = Props.efficiency > 0f ? watts / Props.efficiency : 0f;
            }
            base.CompTick();
        }

        public override string CompInspectStringExtra()
        {
            string baseString = base.CompInspectStringExtra();
            string line = "STB_TurbineLoad".Translate(heatWatts.ToString("0"), Props.capacityWatts.ToString("0"),
                Props.efficiency.ToStringPercent());
            return baseString.NullOrEmpty() ? line : baseString + "\n" + line;
        }
    }
}
