using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TrashbrickBurning
{
    public class CompProperties_HopperFeed : CompProperties
    {
        public CompProperties_HopperFeed()
        {
            compClass = typeof(CompHopperFeed);
        }
    }

    /// <summary>Outlines the intake cells while the building is being placed.</summary>
    public class PlaceWorker_ShowIntake : PlaceWorker
    {
        // VFE Factory's input-rail green, which the sprite's intake chevron also uses.
        public static readonly UnityEngine.Color IntakeColor = new UnityEngine.Color(0.36f, 0.69f, 0.37f);

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, UnityEngine.Color ghostCol, Thing thing = null)
        {
            GenDraw.DrawFieldEdges(new List<IntVec3>(CompHopperFeed.IntakeCells(center, rot, def.size)), IntakeColor);
        }
    }

    /// <summary>
    /// Tops up the parent's CompRefuelable from a hopper standing on its INTAKE: the row of cells
    /// just outside the edge the building faces, which is where its sprite draws the one intake
    /// port (Source/Art/verify_art.py checks the two agree). Any building with isHopper counts -
    /// the vanilla hopper, the Vanilla Furniture Expanded - Factory hopper, or this mod's fuel
    /// hopper. The hopper is the buffer; filling it, by hand or by conveyor, is the player's job,
    /// and this only moves fuel the last cell across. Nothing here references VFE.
    /// </summary>
    public class CompHopperFeed : ThingComp
    {
        private bool feedFromHoppers = true;

        private CompRefuelable refuelable;

        private CompRefuelable Refuelable => refuelable ?? (refuelable = parent.GetComp<CompRefuelable>());

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref feedFromHoppers, "feedFromHoppers", true);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            TryFeed();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(GenTicks.TickRareInterval))
            {
                TryFeed();
            }
        }

        private void TryFeed()
        {
            CompRefuelable fuel = Refuelable;
            if (!feedFromHoppers || fuel == null || !parent.Spawned)
            {
                return;
            }

            Map map = parent.Map;
            foreach (IntVec3 cell in IntakeCells(parent.Position, parent.Rotation, parent.def.size))
            {
                if (fuel.Fuel >= fuel.TargetFuelLevel)
                {
                    return;
                }
                if (!cell.InBounds(map) || !IsHopperAt(cell, map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    if (fuel.Fuel >= fuel.TargetFuelLevel)
                    {
                        return;
                    }
                    Thing thing = things[i];
                    if (thing.def.category != ThingCategory.Item || !fuel.Props.fuelFilter.Allows(thing)
                        || !(parent.GetComp<CompStirlingEngine>()?.Accepts(thing.def) ?? true))
                    {
                        continue;
                    }
                    // Refuel(List<Thing>) takes only what it needs, applies the difficulty fuel
                    // multiplier, and splits off and destroys what it used.
                    fuel.Refuel(new List<Thing> { thing });
                }
            }
        }

        /// <summary>The cells just outside the building's front edge, i.e. in its FacingCell direction.</summary>
        public static IEnumerable<IntVec3> IntakeCells(IntVec3 center, Rot4 rot, IntVec2 size)
        {
            CellRect rect = GenAdj.OccupiedRect(center, rot, size);
            foreach (IntVec3 edge in rect.GetEdgeCells(rot))
            {
                yield return edge + rot.FacingCell;
            }
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            GenDraw.DrawFieldEdges(new List<IntVec3>(IntakeCells(parent.Position, parent.Rotation, parent.def.size)),
                PlaceWorker_ShowIntake.IntakeColor);
        }

        private static bool IsHopperAt(IntVec3 cell, Map map)
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i].def.building != null && things[i].def.building.isHopper)
                {
                    return true;
                }
            }
            return false;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            if (parent.Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "STB_FeedFromHoppers".Translate(),
                    defaultDesc = "STB_FeedFromHoppersDesc".Translate(),
                    icon = TexCommand.ForbidOff,
                    isActive = () => feedFromHoppers,
                    toggleAction = () => feedFromHoppers = !feedFromHoppers
                };
            }
        }
    }
}
