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
    ///   0. accumulators set to reserve charge first, and discharge only when the burners stop;
    ///   1. turbines take what they can, up to their combined capacity;
    ///   2. accumulators discharge into whatever turbine capacity the burners left unfilled;
    ///   3. radiators take what their rooms need;
    ///   4. accumulators charge from what's still left;
    ///   5. the rest goes back to the burners as surplus - DBH hot water may take it (the bridge
    ///      assembly), and whatever nobody takes builds pressure.
    ///
    /// Before any of that, with DBH, each burner's hot water share has already been offered to DBH;
    /// the network only gets what DBH didn't draw of it (CompStirlingEngine.NetworkHeatWatts).
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
            public float priorityChargeCapacity;
            public float priorityCharge;
            public float reserveDischargeCapacity;
            public float reserveDischarge;
            /// <summary>Burners without a safety valve piped to this network, lit or not.</summary>
            public int cobbledBurners;
            public float leftover;
            public bool anyConsumer;

            /// <summary>Everything the turbines turn: burner heat, buffer discharge and reserve discharge.</summary>
            public float TurbineHeat => toTurbines + discharge + reserveDischarge;
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
                if (engine != null && !engine.Props.safetyValve)
                {
                    f.cobbledBurners++;
                }
                if (engine != null && engine.NetworkHeatWatts > 0f)
                {
                    f.heat += engine.NetworkHeatWatts;
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
                    // Each tank sits in at most one charge pool and one discharge pool: a reserve tank
                    // below its reserve level charges first; above it, it behaves like a buffer.
                    f.priorityChargeCapacity += acc.ReserveChargeRoomWatts;
                    f.chargeCapacity += acc.BufferChargeRoomWatts;
                    f.dischargeCapacity += acc.BufferDischargeWatts;
                    f.reserveDischargeCapacity += acc.ReserveDischargeWatts;
                }
            }
            // Reserve accumulators charge before anything else...
            f.priorityCharge = Mathf.Min(f.heat, f.priorityChargeCapacity);
            float rest = f.heat - f.priorityCharge;
            f.toTurbines = Mathf.Min(rest, f.turbineCapacity);
            f.discharge = Mathf.Min(f.turbineCapacity - f.toTurbines, f.dischargeCapacity);
            // ...and give it back only once the burners have stopped, so they never charge and
            // discharge in the same breath.
            if (f.heat <= 0f)
            {
                f.reserveDischarge = Mathf.Min(f.turbineCapacity - f.toTurbines - f.discharge, f.reserveDischargeCapacity);
            }
            rest -= f.toTurbines;
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
            float heat = engine.NetworkHeatWatts;
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
            heatWatts = f.TurbineHeat * Props.capacityWatts / f.turbineCapacity;
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

    /// <summary>
    /// The radiators' thermostat. Vanilla's readout ends in a line break meant for the power line
    /// that follows on a heater; a radiator has no power comp, so RimWorld logs it as an error.
    /// </summary>
    public class CompRadiatorTempControl : CompTempControl
    {
        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra()?.TrimEnd();
        }
    }

    public class CompProperties_HeatAccumulator : CompProperties
    {
        /// <summary>Stored heat, in watt-days.</summary>
        public float capacityWattDays = 1000f;

        /// <summary>How fast it charges and discharges.</summary>
        public float rateWatts = 750f;

        /// <summary>Mean days between explosions when full, freshly bled, on a network of gasifiers only.</summary>
        public float explosionMtbDaysWhenFull = 60f;

        /// <summary>Risk multiplier while any burner without a safety valve is piped to the network.</summary>
        public float cobbledRiskFactor = 4f;

        /// <summary>Wear adds this much risk (x1) every this many days since the tank was last bled, up to x3.</summary>
        public float wearDays = 20f;

        /// <summary>Below this fill the tank can't explode.</summary>
        public float safeFill = 0.2f;

        /// <summary>Blast radius from empty to full.</summary>
        public FloatRange explosionRadius = new FloatRange(2.9f, 6.9f);

        /// <summary>Room heat pushed per watt-day bled off.</summary>
        public float bleedHeatPerWattDay = 0.5f;

        public CompProperties_HeatAccumulator()
        {
            compClass = typeof(CompHeatAccumulator);
        }
    }

    /// <summary>
    /// An insulated tank of pressurised hot water. Two modes:
    ///
    ///   buffer  (default) charges from heat nothing else on the network is using, and gives it back
    ///           to turbines whenever the burners fall short of what they could take;
    ///   reserve charges FIRST, before the turbines, and gives it back only once the burners have
    ///           stopped - out of fuel, switched off, broken - to keep the turbines turning.
    ///           The reserve can be 25/50/75/100% of the tank; above that level it acts as a buffer.
    ///
    /// A battery for steam. Its readout says why it's idle, because an accumulator on a network
    /// whose turbines take every watt never charges in buffer mode, and otherwise that looks broken.
    /// </summary>
    public class CompHeatAccumulator : ThingComp
    {
        public bool reserve;
        public float reserveLevel = 1f;
        public bool bleedRequested;
        public float bleedTo = 0.25f;

        private int lastBledTick = -1;
        private int cobbledOnNet;

        public static readonly float[] ReserveLevels = { 0.25f, 0.5f, 0.75f, 1f };

        private float stored;
        private float lastFlow;
        private string idleReason;

        public CompProperties_HeatAccumulator Props => (CompProperties_HeatAccumulator)props;

        public float Fraction => Props.capacityWattDays > 0f ? stored / Props.capacityWattDays : 0f;

        private float ReserveWattDays => reserve ? Props.capacityWattDays * reserveLevel : 0f;

        public float ChargeRoomWatts => stored < Props.capacityWattDays - 0.01f ? Props.rateWatts : 0f;

        /// <summary>Charges before the turbines: a reserve tank below its reserve level.</summary>
        public float ReserveChargeRoomWatts => reserve && stored < ReserveWattDays - 0.01f ? Props.rateWatts : 0f;

        /// <summary>Charges from spare heat: a buffer tank, or a reserve tank already at its reserve level.</summary>
        public float BufferChargeRoomWatts => ReserveChargeRoomWatts > 0f ? 0f : ChargeRoomWatts;

        /// <summary>Tops the turbines up whenever the burners fall short: whatever sits above the reserve.</summary>
        public float BufferDischargeWatts => stored > ReserveWattDays + 0.01f ? Props.rateWatts : 0f;

        /// <summary>Only once the burners stop: the reserve itself.</summary>
        public float ReserveDischargeWatts => reserve && BufferDischargeWatts <= 0f && stored > 0.01f ? Props.rateWatts : 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref stored, "storedHeat", 0f);
            Scribe_Values.Look(ref reserve, "reserve", false);
            Scribe_Values.Look(ref reserveLevel, "reserveLevel", 1f);
            Scribe_Values.Look(ref bleedRequested, "bleedRequested", false);
            Scribe_Values.Look(ref bleedTo, "bleedTo", 0.25f);
            Scribe_Values.Look(ref lastBledTick, "lastBledTick", -1);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(60))
            {
                return;
            }
            lastFlow = 0f;
            idleReason = null;
            if (lastBledTick < 0)
            {
                lastBledTick = Find.TickManager.TicksGame;
            }
            PipeNet net = HeatNetwork.NetOf(parent);
            cobbledOnNet = 0;
            if (!TrashbrickBurningMod.Advanced || net == null)
            {
                idleReason = "STB_AccIdleNoPipe";
                return;
            }
            HeatNetwork.Flow f = HeatNetwork.Compute(net);
            cobbledOnNet = f.cobbledBurners;
            if (Rand.Chance(ExplosionChancePerDay * 60f / GenDate.TicksPerDay))
            {
                AccumulatorExplosion.Explode(this);
                return;
            }
            float charge = Share(f.priorityCharge, ReserveChargeRoomWatts, f.priorityChargeCapacity)
                + Share(f.charge, BufferChargeRoomWatts, f.chargeCapacity);
            float discharge = Share(f.discharge, BufferDischargeWatts, f.dischargeCapacity)
                + Share(f.reserveDischarge, ReserveDischargeWatts, f.reserveDischargeCapacity);
            lastFlow = charge - discharge;
            stored = Mathf.Clamp(stored + lastFlow * 60f / GenDate.TicksPerDay, 0f, Props.capacityWattDays);
            if (Mathf.Abs(lastFlow) < 0.5f)
            {
                if (f.heat <= 0f && stored <= 0.01f)
                {
                    idleReason = "STB_AccIdleNoHeat";
                }
                else if (ChargeRoomWatts <= 0f && f.heat > 0f)
                {
                    idleReason = "STB_AccIdleFull";
                }
                else if (f.heat > 0f)
                {
                    idleReason = "STB_AccIdleTurbinesTakeAll";
                }
                else if (f.heat <= 0f && f.turbineCapacity <= 0f)
                {
                    idleReason = "STB_AccIdleNoTurbine";
                }
            }
        }

        public float DaysSinceBled => lastBledTick < 0 ? 0f : (Find.TickManager.TicksGame - lastBledTick) / (float)GenDate.TicksPerDay;

        /// <summary>1 when freshly bled, rising to 3 as the seals and valves wear.</summary>
        public float WearFactor => Mathf.Min(3f, 1f + DaysSinceBled / Props.wearDays);

        /// <summary>
        /// Chance a day of the tank letting go. Zero below the safe fill, then rising with the square of
        /// how full it is, times wear, times four with a cobbled burner (no safety valve) on the network.
        /// </summary>
        public float ExplosionChancePerDay
        {
            get
            {
                if (!TrashbrickBurningMod.Advanced || !TrashbrickBurningMod.Hazards || Fraction <= Props.safeFill)
                {
                    return 0f;
                }
                float p = Mathf.InverseLerp(Props.safeFill, 1f, Fraction);
                float risk = p * p * WearFactor / Props.explosionMtbDaysWhenFull;
                if (cobbledOnNet > 0)
                {
                    risk *= Props.cobbledRiskFactor;
                }
                return Mathf.Min(risk, 1f);
            }
        }

        public float BlastRadius => Props.explosionRadius.LerpThroughRange(Fraction);

        public float StoredWattDays => stored;

        public bool NeedsBleed => bleedRequested && Fraction > bleedTo + 0.01f;

        /// <summary>A pawn opens the blow-off valve: the heat goes into the room, the wear resets.</summary>
        public void Bleed()
        {
            float target = Props.capacityWattDays * bleedTo;
            float bled = Mathf.Max(0f, stored - target);
            stored -= bled;
            lastBledTick = Find.TickManager.TicksGame;
            bleedRequested = false;
            if (parent.Spawned)
            {
                GenTemperature.PushHeat(parent.Position, parent.Map, bled * Props.bleedHeatPerWattDay);
                for (int i = 0; i < 6; i++)
                {
                    FleckMaker.ThrowSmoke(parent.TrueCenter(), parent.Map, Rand.Range(1f, 1.8f));
                }
            }
        }

        /// <summary>Heat in or out from outside the steam network: Dubs Bad Hygiene hot water, when it's a tank.</summary>
        public void AddHeat(float wattDays)
        {
            stored = Mathf.Clamp(stored + wattDays, 0f, Props.capacityWattDays);
        }

        public void Emptied()
        {
            stored = 0f;
            bleedRequested = false;
            lastBledTick = Find.TickManager.TicksGame;
        }

        private static float Share(float flow, float mine, float pool) => pool > 0f ? flow * mine / pool : 0f;

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
            yield return new Command_Toggle
            {
                defaultLabel = (reserve ? "STB_AccModeReserve" : "STB_AccModeBuffer").Translate(),
                defaultDesc = "STB_AccModeDesc".Translate(),
                icon = TexCommand.ForbidOff,
                isActive = () => reserve,
                toggleAction = () => reserve = !reserve
            };
            if (reserve)
            {
                yield return new Command_ReserveLevel(this);
            }
            if (TrashbrickBurningMod.Hazards)
            {
                yield return new Command_BleedAccumulator(this);
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            GenDraw.FillableBarRequest bar = default(GenDraw.FillableBarRequest);
            bar.center = parent.DrawPos + Vector3.up * 0.1f + new Vector3(0f, 0f, -0.6f);
            bar.size = new Vector2(1.4f, 0.16f);
            bar.fillPercent = Fraction;
            bar.filledMat = SolidColorMaterials.SimpleSolidColorMaterial(
                Color.Lerp(new Color(0.8f, 0.4f, 0.26f), new Color(0.95f, 0.1f, 0.1f), Mathf.Clamp01(ExplosionChancePerDay * 5f)));
            bar.unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));
            bar.margin = 0.12f;
            GenDraw.DrawFillableBar(bar);
        }

        public override string CompInspectStringExtra()
        {
            // Watt-days to kilowatt-hours, which players read more easily.
            string s = "STB_AccumulatorStatus".Translate((stored * 24f / 1000f).ToString("0.0"),
                (Props.capacityWattDays * 24f / 1000f).ToString("0.0"), lastFlow.ToString("+0;-0;0"));
            s += "\n" + (reserve
                ? "STB_AccModeReserveLevel".Translate(reserveLevel.ToStringPercent()).Resolve()
                : "STB_AccModeBuffer".Translate().Resolve());
            if (idleReason != null)
            {
                s += "\n" + idleReason.Translate();
            }
            if (TrashbrickBurningMod.Advanced && TrashbrickBurningMod.Hazards)
            {
                float risk = ExplosionChancePerDay;
                s += "\n" + (risk > 0f
                    ? "STB_AccRisk".Translate(risk.ToStringPercent("0.#"), BlastRadius.ToString("0"))
                    : "STB_AccRiskNone".Translate(Props.safeFill.ToStringPercent())).Resolve();
                s += "\n" + "STB_AccWear".Translate(DaysSinceBled.ToString("0.0"), WearFactor.ToString("0.0")).Resolve();
                if (cobbledOnNet > 0)
                {
                    s += " " + "STB_AccCobbled".Translate(Props.cobbledRiskFactor.ToString("0")).Resolve();
                }
                if (bleedRequested)
                {
                    s += "\n" + "STB_AccBleedPending".Translate(bleedTo.ToStringPercent()).Resolve();
                }
            }
            return s;
        }
    }

    /// <summary>Left-click steps the reserve 25 → 50 → 75 → 100%; right-click picks one.</summary>
    public class Command_ReserveLevel : Command_Action
    {
        private readonly CompHeatAccumulator acc;

        public Command_ReserveLevel(CompHeatAccumulator acc)
        {
            this.acc = acc;
            defaultLabel = "STB_AccReserveLevel".Translate(acc.reserveLevel.ToStringPercent());
            defaultDesc = "STB_AccReserveLevelDesc".Translate();
            icon = TexCommand.ForbidOff;
            action = () =>
            {
                int i = System.Array.IndexOf(CompHeatAccumulator.ReserveLevels, acc.reserveLevel);
                acc.reserveLevel = CompHeatAccumulator.ReserveLevels[(i + 1) % CompHeatAccumulator.ReserveLevels.Length];
            };
        }

        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions
        {
            get
            {
                foreach (float level in CompHeatAccumulator.ReserveLevels)
                {
                    float l = level;
                    yield return new FloatMenuOption(l.ToStringPercent(), () => acc.reserveLevel = l);
                }
            }
        }
    }
}
