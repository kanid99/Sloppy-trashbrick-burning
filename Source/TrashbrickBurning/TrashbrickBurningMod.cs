using RimWorld;
using UnityEngine;
using Verse;

namespace TrashbrickBurning
{
    public class TrashbrickSettings : ModSettings
    {
        /// <summary>
        /// Advanced: stoves are burners (250/350/500W of heat) that feed steam turbines; their own
        /// Stirling engine makes only 100-300W. Simple: stoves are self-contained generators and the
        /// turbine network is hidden.
        /// </summary>
        public bool advanced = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref advanced, "advanced", true);
        }
    }

    public class TrashbrickBurningMod : Mod
    {
        public static TrashbrickSettings Settings;

        public TrashbrickBurningMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<TrashbrickSettings>();
        }

        public static bool Advanced => Settings == null || Settings.advanced;

        public override string SettingsCategory() => "STB_SettingsCategory".Translate();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);
            list.CheckboxLabeled("STB_SettingAdvanced".Translate(), ref Settings.advanced, "STB_SettingAdvancedDesc".Translate());
            list.Gap();
            list.Label(Settings.advanced ? "STB_SettingAdvancedExplain".Translate() : "STB_SettingSimpleExplain".Translate());
            list.Gap();
            list.Label("STB_SettingRestart".Translate());
            list.End();
        }
    }

    /// <summary>In simple play mode the turbine network isn't part of the game: take it off the build menu.</summary>
    [StaticConstructorOnStartup]
    public static class ApplyPlayMode
    {
        private static readonly string[] AdvancedOnly =
        {
            "STB_SteamTurbine", "STB_HotWaterPipe", "STB_HotWaterPipeHidden", "STB_HotWaterValve"
        };

        static ApplyPlayMode()
        {
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
    }
}
