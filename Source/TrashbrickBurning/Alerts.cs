using System.Collections.Generic;
using RimWorld;
using Verse;

namespace TrashbrickBurning
{
    // The ways the heat network quietly goes wrong. RimWorld finds alerts by reflection and checks
    // one per frame in turn, so a scan of the player's buildings per check is well within what its
    // own alerts do - the same reasoning as the Riimba mod's alerts.
    internal static class AlertUtility
    {
        public static IEnumerable<ThingWithComps> PlayerBuildingsWith<T>() where T : ThingComp
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                List<Building> buildings = maps[i].listerBuildings.allBuildingsColonist;
                for (int j = 0; j < buildings.Count; j++)
                {
                    if (buildings[j].GetComp<T>() != null)
                    {
                        yield return buildings[j];
                    }
                }
            }
        }
    }

    /// <summary>A burner without a safety valve heading for a steam burst.</summary>
    public class Alert_STBOverpressure : Alert_Critical
    {
        private readonly List<Thing> culprits = new List<Thing>();

        public Alert_STBOverpressure()
        {
            defaultLabel = "STB_AlertOverpressure".Translate();
            defaultExplanation = "STB_AlertOverpressureDesc".Translate();
        }

        public override AlertReport GetReport()
        {
            culprits.Clear();
            if (!TrashbrickBurningMod.Advanced || !TrashbrickBurningMod.Hazards)
            {
                return false;
            }
            foreach (ThingWithComps b in AlertUtility.PlayerBuildingsWith<CompStirlingEngine>())
            {
                CompStirlingEngine e = b.GetComp<CompStirlingEngine>();
                if (!e.Props.safetyValve && e.pressure >= CompStirlingEngine.WarnAt)
                {
                    culprits.Add(b);
                }
            }
            return AlertReport.CulpritsAre(culprits);
        }
    }

    /// <summary>A turbine that's switched on but getting no heat.</summary>
    public class Alert_STBTurbineIdle : Alert
    {
        private readonly List<Thing> culprits = new List<Thing>();

        public Alert_STBTurbineIdle()
        {
            defaultLabel = "STB_AlertTurbineIdle".Translate();
            defaultExplanation = "STB_AlertTurbineIdleDesc".Translate();
        }

        public override AlertReport GetReport()
        {
            culprits.Clear();
            if (!TrashbrickBurningMod.Advanced)
            {
                return false;
            }
            foreach (ThingWithComps b in AlertUtility.PlayerBuildingsWith<CompSteamTurbine>())
            {
                CompSteamTurbine t = b.GetComp<CompSteamTurbine>();
                if (t.CanRun && t.HeatWatts < 1f)
                {
                    culprits.Add(b);
                }
            }
            return AlertReport.CulpritsAre(culprits);
        }
    }

    /// <summary>A burner that feeds a network and has run dry.</summary>
    public class Alert_STBBurnerOutOfFuel : Alert
    {
        private readonly List<Thing> culprits = new List<Thing>();

        public Alert_STBBurnerOutOfFuel()
        {
            defaultLabel = "STB_AlertOutOfFuel".Translate();
            defaultExplanation = "STB_AlertOutOfFuelDesc".Translate();
        }

        public override AlertReport GetReport()
        {
            culprits.Clear();
            foreach (ThingWithComps b in AlertUtility.PlayerBuildingsWith<CompStirlingEngine>())
            {
                CompRefuelable fuel = b.GetComp<CompRefuelable>();
                if (fuel != null && !fuel.HasFuel && FlickUtility.WantsToBeOn(b))
                {
                    culprits.Add(b);
                }
            }
            return AlertReport.CulpritsAre(culprits);
        }
    }
}
