using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            new Harmony("kanid99.SloppyTrashbrickBurning").PatchAll();
        }
    }

    /// <summary>
    /// Burners count other fuels at their own values - a wood log is worth less than a trashbrick,
    /// a unit of chemfuel more. CompRefuelable counts every item as one unit, so for our burners
    /// only, this does its item-by-item refuel instead, taking just enough of each stack to fill up.
    /// Everything else still goes through vanilla. Hauled fuel, hopper feed and the fuel gizmo all
    /// come through here; a VFE conveyor refuels by count directly and so counts every item as one.
    /// </summary>
    [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.Refuel), new[] { typeof(List<Thing>) })]
    public static class Patch_CompRefuelable_Refuel
    {
        public static bool Prefix(CompRefuelable __instance, List<Thing> fuelThings)
        {
            CompStirlingEngine engine = __instance.parent.GetComp<CompStirlingEngine>();
            if (engine == null || fuelThings == null)
            {
                return true;
            }
            float room = __instance.Props.fuelCapacity - __instance.Fuel;
            while (room > 0.01f && fuelThings.Count > 0)
            {
                Thing thing = fuelThings[fuelThings.Count - 1];
                fuelThings.RemoveAt(fuelThings.Count - 1);
                float value = Mathf.Max(0.01f, engine.Props.FuelValueOf(thing.def));
                int count = Mathf.Min(thing.stackCount, Mathf.Max(1, Mathf.CeilToInt(room / value)));
                __instance.Refuel(count * value);
                thing.SplitOff(count).Destroy();
                room -= count * value;
            }
            return false;
        }
    }
}
