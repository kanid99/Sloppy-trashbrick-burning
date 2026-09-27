using System.Collections.Generic;
using PipeSystem;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    /// <summary>
    /// Shares the heat on one pressurised hot water network, once per network per tick, read by
    /// every burner, turbine, radiator and accumulator on it. The order:
    ///
    ///   1. turbines take what they can, up to their combined capacity;
    ///   2. accumulators discharge into whatever turbine capacity the burners left unfilled;
    ///   3. radiators take what their rooms need;
    ///   4. accumulators charge from what's still left;
    ///   5. the rest goes back to the burners as surplus - DBH hot water may take it (the bridge
    ///      assembly), and whatever nobody takes builds pressure.
    ///
    /// Vanilla Expanded Framework's PipeSystem carries the pipes and networks; its own resource
    /// traders switch a consumer fully on or off, which would idle a turbine short of heat, so the
    /// sums are done here instead.
    /// </summary>
    public static class HeatNetwork
    {
        public class Flow
        {
            public float heat;
            public int burners;
            public float turbineCapacity;
            public float toTurbines;
            public float discharge;
            public float dischargeCapacity;
            public float radiatorDemand;
            public float toRadiators;
            public float chargeCapacity;
            public float charge;
            public float leftover;
            public bool anyConsumer;
        }

        private static readonly Dictionary<PipeNet, Flow> Cache = new Dictionary<PipeNet, Flow>();
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

        public static IEnumerable<ThingWithComps> Members(PipeNet net)
        {
            HashSet<Thing> seen = new HashSet<Thing>();
            foreach (CompResource connector in net.connectors)
            {
                if (seen.Add(connector.parent))
                {
                    yield return connector.parent;
                }
            }
        }

        public static Flow Compute(PipeNet net)
        {
            int tick = Find.TickManager.TicksGame;
            if (tick != cacheTick)
            {
                Cache.Clear();
                cacheTick = tick;
            }
            if (Cache.TryGetValue(net, out Flow cached))
            {
                return cached;
            }
            Flow f = new Flow();
            foreach (ThingWithComps thing in Members(net))
            {
                CompStirlingEngine engine = thing.GetComp<CompStirlingEngine>();
                if (engine != null && engine.HeatWatts > 0f)
                {
                    f.heat += engine.HeatWatts;
                    f.burners++;
                }
                CompSteamTurbine turbine = thing.GetComp<CompSteamTurbine>();
                if (turbine != null)
                {
                    f.anyConsumer = true;
                    if (turbine.CanRun)
                    {
                        f.turbineCapacity += turbine.Props.capacityWatts;
                    }
                }
                CompHotWaterRadiator radiator = thing.GetComp<CompHotWaterRadiator>();
                if (radiator != null)
                {
                    f.anyConsumer = true;
                    f.radiatorDemand += radiator.Demand;
                }
                CompHeatAccumulator acc = thing.GetComp<CompHeatAccumulator>();
                if (acc != null)
                {
                    f.anyConsumer = true;
                    f.chargeCapacity += acc.ChargeRoomWatts;
                    f.dischargeCapacity += acc.DischargeAvailableWatts;
                }
            }
            f.toTurbines = Mathf.Min(f.heat, f.turbineCapacity);
            f.discharge = Mathf.Min(f.turbineCapacity - f.toTurbines, f.dischargeCapacity);
            float rest = f.heat - f.toTurbines;
            f.toRadiators = Mathf.Min(rest, f.radiatorDemand);
            rest -= f.toRadiators;
            f.charge = Mathf.Min(rest, f.chargeCapacity);
            rest -= f.charge;
            f.leftover = rest;
            Cache[net] = f;
            return f;
        }

        public static void UpdateBurner(CompStirlingEngine engine)
        {
            float heat = engine.HeatWatts;
            PipeNet net = NetOf(engine.parent);
            if (net == null)
            {
                engine.networkConnected = false;
                engine.toNetworkWatts = 0f;
                engine.surplusWatts = heat;
                return;
            }
            Flow f = Compute(net);
            engine.networkConnected = f.anyConsumer;
            float share = f.heat > 0f ? heat / f.heat : 0f;
            engine.surplusWatts = f.leftover * share;
            engine.toNetworkWatts = heat - engine.surplusWatts;
        }
    }

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

    /// <summary>A generator that turns the heat piped to it into power at its efficiency, up to its capacity.</summary>
    public class CompSteamTurbine : CompPowerPlant
    {
        private const int RecalcInterval = 60;

        private float watts;
        private float heatWatts;
        private int feeders;

        public new CompProperties_SteamTurbine Props => (CompProperties_SteamTurbine)props;

        protected override float DesiredPowerOutput => watts;

        public float HeatWatts => heatWatts;

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
                Recalculate();
            }
            base.CompTick();
        }

        private void Recalculate()
        {
            watts = heatWatts = 0f;
            feeders = 0;
            PipeNet net = HeatNetwork.NetOf(parent);
            if (!TrashbrickBurningMod.Advanced || net == null || !CanRun)
            {
                return;
            }
            HeatNetwork.Flow f = HeatNetwork.Compute(net);
            feeders = f.burners;
            if (f.turbineCapacity <= 0f)
            {
                return;
            }
            heatWatts = (f.toTurbines + f.discharge) * Props.capacityWatts / f.turbineCapacity;
            watts = heatWatts * Props.efficiency * TrashbrickBurningMod.S.powerMultiplier;
        }

        public override string CompInspectStringExtra()
        {
            string baseString = base.CompInspectStringExtra();
            string line = "STB_TurbineLoad".Translate(heatWatts.ToString("0"), Props.capacityWatts.ToString("0"),
                Props.efficiency.ToStringPercent()) + "\n" + "STB_TurbineFeeders".Translate(feeders);
            return baseString.NullOrEmpty() ? line : baseString + "\n" + line;
        }
    }

    public class CompProperties_HotWaterRadiator : CompProperties
    {
        /// <summary>The most heat it takes from the network.</summary>
        public float maxWatts = 250f;

        /// <summary>Room heat pushed per second for each watt received. Vanilla's heater is about 0.12.</summary>
        public float heatPerWattSecond = 0.06f;

        public CompProperties_HotWaterRadiator()
        {
            compClass = typeof(CompHotWaterRadiator);
        }
    }

    /// <summary>
    /// Heats its room from the network, up to the target temperature set on its CompTempControl, with
    /// vanilla's own temperature system - so it sits alongside Vanilla Temperature Expanded or any
    /// other heater. Radiators come after turbines in the network's order.
    /// </summary>
    public class CompHotWaterRadiator : ThingComp
    {
        private float received;

        public CompProperties_HotWaterRadiator Props => (CompProperties_HotWaterRadiator)props;

        public float Demand
        {
            get
            {
                if (!parent.Spawned || !FlickUtility.WantsToBeOn(parent))
                {
                    return 0f;
                }
                CompTempControl temp = parent.GetComp<CompTempControl>();
                Room room = parent.GetRoom();
                if (room == null || room.UsesOutdoorTemperature)
                {
                    return 0f;
                }
                float target = temp?.targetTemperature ?? 21f;
                return room.Temperature < target ? Props.maxWatts : 0f;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(60))
            {
                return;
            }
            received = 0f;
            PipeNet net = HeatNetwork.NetOf(parent);
            if (!TrashbrickBurningMod.Advanced || net == null)
            {
                return;
            }
            HeatNetwork.Flow f = HeatNetwork.Compute(net);
            float demand = Demand;
            if (demand > 0f && f.radiatorDemand > 0f)
            {
                received = f.toRadiators * demand / f.radiatorDemand;
                GenTemperature.PushHeat(parent, received * Props.heatPerWattSecond);
            }
        }

        public override string CompInspectStringExtra()
        {
            return "STB_RadiatorStatus".Translate(received.ToString("0"), Props.maxWatts.ToString("0"));
        }
    }

    public class CompProperties_HeatAccumulator : CompProperties
    {
        /// <summary>Stored heat, in watt-days.</summary>
        public float capacityWattDays = 1000f;

        /// <summary>How fast it charges and discharges.</summary>
        public float rateWatts = 750f;

        public CompProperties_HeatAccumulator()
        {
            compClass = typeof(CompHeatAccumulator);
        }
    }

    /// <summary>
    /// An insulated tank of pressurised hot water: charges from heat nothing else on the network is
    /// using, and gives it back to turbines when the burners fall short - so a turbine keeps turning
    /// while its burners are refuelled or switched off. A battery for steam.
    /// </summary>
    public class CompHeatAccumulator : ThingComp
    {
        private float stored;
        private float lastFlow;

        public CompProperties_HeatAccumulator Props => (CompProperties_HeatAccumulator)props;

        public float Fraction => Props.capacityWattDays > 0f ? stored / Props.capacityWattDays : 0f;

        public float ChargeRoomWatts => stored < Props.capacityWattDays - 0.01f ? Props.rateWatts : 0f;

        public float DischargeAvailableWatts => stored > 0.01f ? Props.rateWatts : 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref stored, "storedHeat", 0f);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(60))
            {
                return;
            }
            lastFlow = 0f;
            PipeNet net = HeatNetwork.NetOf(parent);
            if (!TrashbrickBurningMod.Advanced || net == null)
            {
                return;
            }
            HeatNetwork.Flow f = HeatNetwork.Compute(net);
            float charge = f.chargeCapacity > 0f ? f.charge * ChargeRoomWatts / f.chargeCapacity : 0f;
            float discharge = f.dischargeCapacity > 0f ? f.discharge * DischargeAvailableWatts / f.dischargeCapacity : 0f;
            lastFlow = charge - discharge;
            stored = Mathf.Clamp(stored + lastFlow * 60f / GenDate.TicksPerDay, 0f, Props.capacityWattDays);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            GenDraw.FillableBarRequest bar = default(GenDraw.FillableBarRequest);
            bar.center = parent.DrawPos + Vector3.up * 0.1f + new Vector3(0f, 0f, -0.6f);
            bar.size = new Vector2(1.4f, 0.16f);
            bar.fillPercent = Fraction;
            bar.filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.8f, 0.4f, 0.26f));
            bar.unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));
            bar.margin = 0.12f;
            GenDraw.DrawFillableBar(bar);
        }

        public override string CompInspectStringExtra()
        {
            // Watt-days to kilowatt-hours, which players read more easily.
            return "STB_AccumulatorStatus".Translate((stored * 24f / 1000f).ToString("0.0"),
                (Props.capacityWattDays * 24f / 1000f).ToString("0.0"), lastFlow.ToString("+0;-0;0"));
        }
    }
}
