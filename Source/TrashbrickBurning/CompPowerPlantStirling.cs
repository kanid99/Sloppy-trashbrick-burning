using RimWorld;
using Verse;

namespace TrashbrickBurning
{
    /// <summary>
    /// A fuelled power plant whose output CompStirlingEngine controls: scaled by
    /// <see cref="outputFactor"/> in simple play mode, or set outright by <see cref="fixedWatts"/>
    /// in advanced (the small built-in engine, or nothing while feeding a turbine).
    /// </summary>
    public class CompPowerPlantStirling : CompPowerPlant
    {
        public float outputFactor = 1f;

        /// <summary>When set, the output in watts, replacing basePowerConsumption.</summary>
        public float? fixedWatts;

        protected override float DesiredPowerOutput => fixedWatts ?? base.DesiredPowerOutput * outputFactor;
    }
}
