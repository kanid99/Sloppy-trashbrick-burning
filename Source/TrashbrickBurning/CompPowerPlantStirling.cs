using RimWorld;
using Verse;

namespace TrashbrickBurning
{
    /// <summary>
    /// A fuelled power plant whose output can be scaled by something else on the building. With
    /// Dubs Bad Hygiene loaded, the bridge assembly sets <see cref="outputFactor"/> from the
    /// engine's mode: water-cooled runs above 1, heat recovery below it. Without DBH nothing
    /// touches it and this behaves exactly like CompPowerPlant.
    /// </summary>
    public class CompPowerPlantStirling : CompPowerPlant
    {
        public float outputFactor = 1f;

        protected override float DesiredPowerOutput => base.DesiredPowerOutput * outputFactor;
    }
}
