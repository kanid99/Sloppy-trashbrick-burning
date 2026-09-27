using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    public class TrashbrickSettings : ModSettings
    {
        /// <summary>
        /// Advanced: stoves are burners (250/350/500W of heat) that feed steam turbines, radiators and
        /// heat accumulators; their own engine makes only 100-300W. Simple: stoves are self-contained
        /// generators and the heat network is hidden.
        /// </summary>
        public bool advanced = true;

        /// <summary>Overpressure bursts and steam leaks.</summary>
        public bool hazards = true;

        /// <summary>Burners also take wood and chemfuel, at their own fuel values.</summary>
        public bool otherFuels = true;

        public float fuelUseMultiplier = 1f;
        public float powerMultiplier = 1f;
        public float ashMultiplier = 1f;
        public float leakFrequencyMultiplier = 1f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref advanced, "advanced", true);
            Scribe_Values.Look(ref hazards, "hazards", true);
            Scribe_Values.Look(ref otherFuels, "otherFuels", true);
            Scribe_Values.Look(ref fuelUseMultiplier, "fuelUseMultiplier", 1f);
            Scribe_Values.Look(ref powerMultiplier, "powerMultiplier", 1f);
            Scribe_Values.Look(ref ashMultiplier, "ashMultiplier", 1f);
            Scribe_Values.Look(ref leakFrequencyMultiplier, "leakFrequencyMultiplier", 1f);
        }
    }

    public class TrashbrickBurningMod : Mod
    {
        public static TrashbrickSettings Settings;

        public TrashbrickBurningMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<TrashbrickSettings>();
        }

        public static TrashbrickSettings S => Settings ?? (Settings = new TrashbrickSettings());

        public static bool Advanced => S.advanced;

        public static bool Hazards => S.hazards;

        public override string SettingsCategory() => "STB_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            TrashbrickSettings s = S;
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.CheckboxLabeled("STB_SettingAdvanced".Translate(), ref s.advanced, "STB_SettingAdvancedDesc".Translate());
            list.Label(s.advanced ? "STB_SettingAdvancedExplain".Translate() : "STB_SettingSimpleExplain".Translate());
            list.Gap();
            list.CheckboxLabeled("STB_SettingHazards".Translate(), ref s.hazards, "STB_SettingHazardsDesc".Translate());
            list.CheckboxLabeled("STB_SettingOtherFuels".Translate(), ref s.otherFuels, "STB_SettingOtherFuelsDesc".Translate());
            list.Gap();
            Slider(list, "STB_SettingFuelUse", ref s.fuelUseMultiplier);
            Slider(list, "STB_SettingPower", ref s.powerMultiplier);
            Slider(list, "STB_SettingAsh", ref s.ashMultiplier, 0f);
            if (s.hazards)
            {
                Slider(list, "STB_SettingLeaks", ref s.leakFrequencyMultiplier, 0.1f, 5f);
            }
            list.Gap();
            if (list.ButtonText("STB_SettingReset".Translate()))
            {
                s.fuelUseMultiplier = s.powerMultiplier = s.ashMultiplier = s.leakFrequencyMultiplier = 1f;
            }
            list.Gap();
            list.Label("STB_SettingRestart".Translate());
            list.End();
        }

        private static void Slider(Listing_Standard list, string key, ref float value, float min = 0.25f, float max = 3f)
        {
            list.Label(key.Translate(value.ToStringPercent()));
            value = Mathf.Round(list.Slider(value, min, max) * 20f) / 20f;
        }
    }

    /// <summary>
    /// Applies the settings that change defs, once at startup: in simple play mode the heat network
    /// comes off the build menu, and with other fuels switched off the burners and fuel hopper stop
    /// taking wood and chemfuel.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class ApplyPlayMode
    {
        private static readonly string[] AdvancedOnly =
        {
            "STB_SteamTurbine", "STB_CobbledTurbine", "STB_HotWaterPipe", "STB_HotWaterPipeHidden",
            "STB_HotWaterValve", "STB_HotWaterRadiator", "STB_CobbledRadiator", "STB_HeatAccumulator"
        };

        static ApplyPlayMode()
        {
            if (!TrashbrickBurningMod.S.otherFuels)
            {
                RemoveOtherFuels();
            }
            if (TrashbrickBurningMod.Advanced)
            {
                return;
            }
            DesignationCategoryDef category = null;
            foreach (string name in AdvancedOnly)
            {
                ThingDef def = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                if (def == null)
                {
                    continue;
                }
                category = category ?? def.designationCategory;
                def.designationCategory = null;
            }
            // Rebuild the category's designator list without them.
            category?.ResolveReferences();
        }

        private static void RemoveOtherFuels()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                CompProperties_StirlingEngine engine = def.GetCompProperties<CompProperties_StirlingEngine>();
                if (engine == null || engine.otherFuels.NullOrEmpty())
                {
                    continue;
                }
                CompProperties_Refuelable fuel = def.GetCompProperties<CompProperties_Refuelable>();
                foreach (FuelValue other in engine.otherFuels)
                {
                    fuel?.fuelFilter.SetAllow(other.thing, false);
                }
            }
            ThingDef hopper = DefDatabase<ThingDef>.GetNamedSilentFail("STB_FuelHopper");
            if (hopper?.building != null)
            {
                foreach (string name in new[] { "WoodLog", "Chemfuel" })
                {
                    ThingDef other = DefDatabase<ThingDef>.GetNamedSilentFail(name);
                    if (other != null)
                    {
                        hopper.building.fixedStorageSettings?.filter.SetAllow(other, false);
                        hopper.building.defaultStorageSettings?.filter.SetAllow(other, false);
                    }
                }
            }
        }
    }
}
