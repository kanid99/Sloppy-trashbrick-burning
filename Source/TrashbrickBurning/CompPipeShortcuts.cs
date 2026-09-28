using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TrashbrickBurning
{
    public class CompProperties_PipeShortcuts : CompProperties
    {
        public List<ThingDef> defs = new List<ThingDef>();

        public CompProperties_PipeShortcuts()
        {
            compClass = typeof(CompPipeShortcuts);
        }
    }

    /// <summary>
    /// Build buttons for the pressurised hot water pipes and valve on the buildings that connect to
    /// them, so running pipe from a burner to a turbine doesn't mean a trip to the architect menu -
    /// the way vanilla puts "build copy" on a building. They're the architect's own designators, found
    /// through BuildCopyCommandUtility, so they only appear once the pipe is researched and on the
    /// build menu (not in simple play mode), and behave exactly like it.
    /// </summary>
    public class CompPipeShortcuts : ThingComp
    {
        private CompProperties_PipeShortcuts Props => (CompProperties_PipeShortcuts)props;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (parent.Faction != Faction.OfPlayer || !TrashbrickBurningMod.Advanced)
            {
                yield break;
            }
            foreach (ThingDef def in Props.defs)
            {
                Designator_Build designator = BuildCopyCommandUtility.FindAllowedDesignator(def);
                if (designator != null)
                {
                    yield return designator;
                }
            }
        }
    }
}
