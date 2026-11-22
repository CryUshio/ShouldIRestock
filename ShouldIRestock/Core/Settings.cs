using System.Collections.Generic;

namespace ShouldIRestock
{
    public static class Settings
    {
        private static string TypeSection => Text.L("opt_types", "Show these types");

        public static ConfigEntry<bool> IgnoreLocked;
        public static ConfigEntry<bool> SkipDebris;
        public static ConfigEntry<int> Thickness;
        public static ConfigEntry<float> ScanSeconds;
        private static readonly Dictionary<string, ConfigEntry<bool>> Types = new Dictionary<string, ConfigEntry<bool>>();

        public static void Init()
        {
            ModConfig config = ModHelper.GetConfig(ModIds.Id);
            IgnoreLocked = config.Define(
                "ignore_locked",
                true,
                "Stock",
                () => Text.L("opt_ignore_locked", "Skip goods inside locked containers"),
                () => Text.L("opt_ignore_locked_tip", "Locked boxes and items behind a lock are left out of the tally and the outline."));
            SkipDebris = config.Define(
                "skip_debris",
                true,
                "Stock",
                () => Text.L("opt_skip_debris", "Skip debris"),
                () => Text.L("opt_skip_debris_tip", "Piles you cannot move are not counted as stock."));
            Thickness = config.Define(
                "thickness",
                1,
                "Highlight",
                () => Text.L("opt_thickness", "Outline thickness"),
                min: 1,
                max: 6,
                tooltip: () => Text.L("opt_thickness_tip", "Pixel thickness of the item outline."));
            ScanSeconds = config.Define(
                "scan_seconds",
                0.4f,
                "Highlight",
                () => Text.L("opt_scan", "Stock refresh interval (seconds)"),
                min: 0.2f,
                max: 2f,
                tooltip: () => Text.L("opt_scan_tip", "How often the counts and outlines are recalculated."));
            DefineType(config, ModIds.Stolen, true, () => Text.L("all_stolen", "All stolen"));
            DefineType(config, ModIds.Contraband, true, () => Text.L("all_contraband", "All contraband"));
            DefineGroup(config, TypeHelper.MAJOR_TYPES_LIST, true);
            DefineGroup(config, TypeHelper.ALL_TYPES_LIST, false);
            DefineType(config, ModIds.Other, false, () => Text.L("tag_other", "Uncategorized"));
        }

        public static bool Shows(string id)
        {
            ConfigEntry<bool> entry;
            if (id != null && Types.TryGetValue(id, out entry) && entry != null)
            {
                return entry.Value;
            }

            return id == ModIds.Stolen || id == ModIds.Contraband || TypeHelper.MAJOR_TYPES_LIST.Contains(id);
        }

        private static void DefineGroup(ModConfig config, List<string> ids, bool shown)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (Types.ContainsKey(id) || id == ModIds.ContrabandType)
                {
                    continue;
                }

                string captured = id;
                DefineType(config, captured, shown, () => TagCatalog.NameOf(captured));
            }
        }

        private static void DefineType(ModConfig config, string id, bool shown, System.Func<string> label)
        {
            Types[id] = config.Define("show_" + id, shown, TypeSection, label);
        }

        public static int LineThickness => Thickness == null ? 1 : UnityEngine.Mathf.Clamp(Thickness.Value, 1, 6);

        public static float Interval => ScanSeconds == null ? 0.4f : UnityEngine.Mathf.Clamp(ScanSeconds.Value, 0.2f, 2f);
    }
}
