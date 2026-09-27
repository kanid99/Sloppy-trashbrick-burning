using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    /// <summary>
    /// Ash from the burners, put out the way the SloppyMods Riimba station puts out its waste: whole
    /// items, automatically, as soon as they add up, on one spot - the cell behind the burner, the
    /// side opposite its intake. It never blocks: if that cell is taken the ash goes on the nearest
    /// free one, and a hopper or storage there catches it for a hauler or a conveyor. A burner that
    /// stopped when its ash pan filled would cut the power in the night, which reads as a bug.
    /// </summary>
    public static class Ash
    {
        private static ThingDef ashDef;

        private static ThingDef AshDef => ashDef ?? (ashDef = DefDatabase<ThingDef>.GetNamedSilentFail("STB_Ash"));

        public static IntVec3 DropCell(Thing burner)
        {
            Rot4 back = burner.Rotation.Opposite;
            System.Collections.Generic.List<IntVec3> edge =
                new System.Collections.Generic.List<IntVec3>(burner.OccupiedRect().GetEdgeCells(back));
            // The middle of the back edge (the first of the two middle cells on a 2-wide machine).
            return edge.Count == 0 ? burner.Position : edge[(edge.Count - 1) / 2] + back.FacingCell;
        }

        public static void DropWholeUnits(CompStirlingEngine engine, ref float buffer)
        {
            int count = Mathf.FloorToInt(buffer);
            if (count > 0 && Drop(engine.parent, count))
            {
                buffer -= count;
            }
        }

        /// <summary>The dump gizmo: rounds up, and lets the buffer go negative, so it can't mint ash.</summary>
        public static void DropAll(CompStirlingEngine engine, ref float buffer)
        {
            int count = Mathf.CeilToInt(buffer);
            if (count > 0 && Drop(engine.parent, count))
            {
                buffer -= count;
            }
        }

        private static bool Drop(Thing burner, int count)
        {
            ThingDef def = AshDef;
            Map map = burner.Map;
            if (def == null || map == null)
            {
                return false;
            }
            IntVec3 cell = DropCell(burner);
            if (!cell.InBounds(map))
            {
                cell = burner.Position;
            }
            while (count > 0)
            {
                Thing ash = ThingMaker.MakeThing(def);
                ash.stackCount = Mathf.Min(count, def.stackLimit);
                count -= ash.stackCount;
                GenPlace.TryPlaceThing(ash, cell, map, ThingPlaceMode.Near);
            }
            return true;
        }
    }
}
