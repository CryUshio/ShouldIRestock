namespace ShouldIRestock
{
    public static class Settings
    {
        public static ConfigEntry<bool> IgnoreLocked;
        public static ConfigEntry<bool> SkipDebris;
        public static ConfigEntry<int> Thickness;
        public static ConfigEntry<float> ScanSeconds;

        public static void Init()
        {
            ModConfig config = ModHelper.GetConfig(ModIds.Id);
            IgnoreLocked = config.Define(
                "ignore_locked",
                true,
                "Stock",
                () => Text.L("opt_ignore_locked", "跳过上锁容器里的货"),
                () => Text.L("opt_ignore_locked_tip", "锁着的箱子和锁后的物品不计入分类，也不描边。"));
            SkipDebris = config.Define(
                "skip_debris",
                true,
                "Stock",
                () => Text.L("opt_skip_debris", "跳过杂物堆"),
                () => Text.L("opt_skip_debris_tip", "不把没法搬走的杂物算进库存。"));
            Thickness = config.Define(
                "thickness",
                2,
                "Highlight",
                () => Text.L("opt_thickness", "描边粗细"),
                min: 1,
                max: 6,
                tooltip: () => Text.L("opt_thickness_tip", "物品外框的像素粗细。"));
            ScanSeconds = config.Define(
                "scan_seconds",
                0.4f,
                "Highlight",
                () => Text.L("opt_scan", "库存刷新间隔（秒）"),
                min: 0.2f,
                max: 2f,
                tooltip: () => Text.L("opt_scan_tip", "便签上的件数和描边按这个间隔重算。"));
        }

        public static int LineThickness => Thickness == null ? 2 : UnityEngine.Mathf.Clamp(Thickness.Value, 1, 6);

        public static float Interval => ScanSeconds == null ? 0.4f : UnityEngine.Mathf.Clamp(ScanSeconds.Value, 0.2f, 2f);
    }
}
