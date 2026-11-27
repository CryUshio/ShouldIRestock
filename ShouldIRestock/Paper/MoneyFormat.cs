using System.Globalization;
using TMPro;
using UnityEngine;

namespace ShouldIRestock
{
    public static class MoneyFormat
    {
        private static TMP_SpriteAsset creditIcons;

        public static string Text(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture) + GeneralHelper.CreditIcon(true);
        }

        // 游戏的信用点图标是文字网格里名为 c 的贴图，和结算、调试面板用的是同一个。
        public static void Apply(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            TMP_SpriteAsset icons = Icons();
            if (icons != null && text.spriteAsset != icons)
            {
                text.spriteAsset = icons;
            }
        }

        private static TMP_SpriteAsset Icons()
        {
            if (creditIcons != null)
            {
                return creditIcons;
            }

            TMP_SpriteAsset fallback = TMP_Settings.defaultSpriteAsset;
            if (HasCredit(fallback))
            {
                creditIcons = fallback;
                return creditIcons;
            }

            TMP_SpriteAsset[] loaded = Resources.FindObjectsOfTypeAll<TMP_SpriteAsset>();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (HasCredit(loaded[i]))
                {
                    creditIcons = loaded[i];
                    return creditIcons;
                }
            }

            creditIcons = fallback;
            return creditIcons;
        }

        private static bool HasCredit(TMP_SpriteAsset asset)
        {
            return asset != null && asset.GetSpriteIndexFromName("c") >= 0;
        }
    }
}
