using DubCore;
using DubsBadHygiene;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning.DBH
{
    /// <summary>
    /// Makes the Overpressure Tank a Dubs Bad Hygiene boiler: it feeds DBH's hot water tanks and
    /// radiators from the heat it holds, which draws its pressure down. It offers only what those
    /// tanks and radiators are short of - DBH keeps their heat as a 0-1 temperature, so a store at
    /// temperature t is short of (1 - t) of its capacity - so a satisfied plumbing network doesn't
    /// drain it. What DBH draws comes out of the stored heat, watt for watt (1 DBH unit = 1 W, as
    /// DBH's own electric boiler).
    /// </summary>
    public class CompTankBoiler : CompBoiler
    {
        private const int Interval = 60;

        private float drawn;

        private CompHeatAccumulator Tank => parent.GetComp<CompHeatAccumulator>();

        private PlumbingNet Net => parent.GetComp<CompPipe>()?.pipeNet;

        private float Shortfall
        {
            get
            {
                PlumbingNet net = Net;
                if (net?.HeatStores == null)
                {
                    return 0f;
                }
                float shortfall = 0f;
                foreach (HeatStore store in net.HeatStores)
                {
                    shortfall += store.GetStoreCapacity * Mathf.Clamp01(1f - store.HeaterTemp);
                }
                return shortfall;
            }
        }

        public override bool WorkingNow
        {
            get
            {
                CompHeatAccumulator tank = Tank;
                return CompStirlingEngine.Advanced && tank != null && tank.StoredWattDays > 0.5f;
            }
        }

        public override float Capacity
        {
            get
            {
                CompHeatAccumulator tank = Tank;
                return WorkingNow ? Mathf.Min(tank.Props.rateWatts, Shortfall) : 0f;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.IsHashIntervalTick(Interval))
            {
                return;
            }
            drawn = 0f;
            PlumbingNet net = Net;
            CompHeatAccumulator tank = Tank;
            if (net == null || tank == null || !WorkingNow || net.BoilerCapacitySum <= 0f)
            {
                return;
            }
            drawn = Capacity * Mathf.Clamp01(net.HeatStoreCapacitySum / net.BoilerCapacitySum);
            tank.AddHeat(-drawn * Interval / GenDate.TicksPerDay);
        }

        // The electric boiler's power stepper means nothing here.
        public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield break;
        }

        public override string CompInspectStringExtra()
        {
            PlumbingNet net = Net;
            if (net == null)
            {
                return "STB_TankNoPlumbing".Translate();
            }
            return "STB_TankToPlumbing".Translate(drawn.ToString("0"), Shortfall.ToString("0"));
        }
    }
}
