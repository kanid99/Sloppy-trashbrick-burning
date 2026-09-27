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

    /// <summary>
    /// Tops up the parent's CompRefuelable from any hopper touching one of its edges: the vanilla
    /// hopper, the Vanilla Furniture Expanded - Factory hopper, or this mod's fuel hopper. The hopper
    /// is the buffer; filling it (by hand or by conveyor) is the player's problem, and this only moves
    /// fuel the last cell across. Nothing here references VFE, so it works with or without it.
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
            foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(parent))
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
                    if (thing.def.category != ThingCategory.Item || !fuel.Props.fuelFilter.Allows(thing))
                    {
                        continue;
                    }
                    // Refuel(List<Thing>) takes only what it needs, applies the difficulty fuel
                    // multiplier, and splits off and destroys what it used.
                    fuel.Refuel(new List<Thing> { thing });
                }
            }
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
