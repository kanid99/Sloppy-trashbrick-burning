using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TrashbrickBurning
{
    public class CompProperties_BuildShortcuts : CompProperties
    {
        public CompProperties_BuildShortcuts()
        {
            compClass = typeof(CompBuildShortcuts);
        }
    }

    /// <summary>
    /// Build buttons for every other building in the mod, on each of them, so a whole chain - hopper,
    /// burner, pipe, turbine, accumulator, radiator - can be laid out from whatever is selected
    /// without going back to the architect menu. The selected building itself is left out; vanilla's
    /// "build copy" already covers it.
    ///
    /// They're the architect's own designators, found through BuildCopyCommandUtility, so each only
    /// appears once it's researched and on the build menu - the heat network isn't in simple play
    /// mode - and behaves exactly like it, dropdowns and all.
    /// </summary>
    public class CompBuildShortcuts : ThingComp
    {
        // Fuel to power: feed, burn, carry, turn, store, heat.
        private static readonly string[] Chain =
        {
            "STB_FuelHopper",
            "STB_TrashbrickPelletStove", "STB_TrashbrickGasifier",
            "STB_LargeCobbledStove", "STB_LargeGasifier", "STB_IndustrialGasifier",
            "STB_ExhaustPipe", "STB_ExhaustPipeHidden", "STB_ExhaustPort", "STB_ExhaustPortWall",
            "STB_HotWaterPipe", "STB_HotWaterPipeHidden", "STB_HotWaterValve",
            "STB_CobbledTurbine", "STB_SteamTurbine",
            "STB_HeatAccumulator", "STB_SteamVent",
            "STB_CobbledRadiator", "STB_HotWaterRadiator"
        };

        private static List<ThingDef> chainDefs;

        private static List<ThingDef> ChainDefs
        {
            get
            {
                if (chainDefs == null)
                {
                    chainDefs = new List<ThingDef>();
                    foreach (string name in Chain)
                    {
                        ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                        if (def != null)
                        {
                            chainDefs.Add(def);
                        }
                    }
                }
                return chainDefs;
            }
        }

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
            foreach (ThingDef def in ChainDefs)
            {
                if (def == parent.def)
                {
                    continue;
                }
                Designator_Build designator = BuildCopyCommandUtility.FindAllowedDesignator(def);
                if (designator != null)
                {
                    yield return designator;
                }
            }
        }
    }
}
