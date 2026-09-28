using DubsBadHygiene;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning.DBH
{
    /// <summary>
    /// Makes the heat accumulator a Dubs Bad Hygiene hot water tank as well. DBH keeps a tank's
    /// heat as a temperature from 0 to 1; here that temperature IS the accumulator's charge, so a
    /// full accumulator is a hot tank, and showers and baths drawing hot water drain it.
    ///
    /// Each tick: DBH boilers on the plumbing (our burners' hot water share among them) charge it by
    /// its share of their watts, energy for energy, rather than at DBH's flat rise rate; whatever DBH changed since last tick - hot water pulled,
    /// boiler heat added - goes into or out of the accumulator's stored heat; then the temperature
    /// is set back to the accumulator's charge, which the steam network may also have changed.
    /// DBH's own drift towards the boilers' level is left out, because on a tank fuller than the
    /// boilers could make it, that would bleed stored heat away for nothing.
    /// </summary>
    public class CompAccumulatorHeatStore : CompHeatStore
    {
        private float lastSet = -1f;

        public override void CompTick()
        {
            CompHeatAccumulator acc = parent.GetComp<CompHeatAccumulator>();
            if (acc == null)
            {
                base.CompTick();
                return;
            }
            float t = HeaterTemp;
            PlumbingNet net = parent.GetComp<CompPipe>()?.pipeNet;
            if (net != null && t < 1f && net.BoilerCapacitySum > 0f && net.HeatStoreCapacitySum > 0f)
            {
                // Boiler units are watts. This tank's share of them, by its share of the plumbing's
                // storage - the same share CompStirlingWater assumes the tanks take.
                float watts = net.BoilerCapacitySum * GetStoreCapacity / net.HeatStoreCapacitySum;
                t = Mathf.Min(1f, t + watts / GenDate.TicksPerDay / acc.Props.capacityWattDays);
            }
            if (lastSet >= 0f)
            {
                acc.AddHeat((t - lastSet) * acc.Props.capacityWattDays);
            }
            lastSet = acc.Fraction;
            HeaterTemp = lastSet;
        }
    }
}
