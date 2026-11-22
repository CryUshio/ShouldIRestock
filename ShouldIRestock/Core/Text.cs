using System.Collections.Generic;
using UnityEngine.Localization.Settings;

namespace ShouldIRestock
{
    public static class Text
    {
        public static string L(string key, string fallback)
        {
            try
            {
                LoadedMod mod = ModLoader.Mods.Find(m => m.Manifest.ID == ModIds.Id);
                if (mod != null)
                {
                    foreach (string code in LocaleCodes())
                    {
                        Dictionary<string, string> table;
                        string value;
                        if (mod.Localization.TryGetValue(code, out table)
                            && table.TryGetValue(key, out value)
                            && !string.IsNullOrEmpty(value))
                        {
                            return value;
                        }
                    }
                }
            }
            catch
            {
            }

            return fallback;
        }

        public static string F(string key, string fallback, params object[] args)
        {
            try
            {
                return string.Format(L(key, fallback), args);
            }
            catch
            {
                return fallback;
            }
        }

        // 先用游戏当前语言，再试语言主标签（zh-Hans 对应 zh），最后才是英语。
        private static IEnumerable<string> LocaleCodes()
        {
            string code = "en";
            var locale = LocalizationSettings.SelectedLocale;
            if (locale != null && !string.IsNullOrEmpty(locale.Identifier.Code))
            {
                code = locale.Identifier.Code;
            }

            yield return code;
            int dash = code.IndexOf('-');
            if (dash > 0)
            {
                yield return code.Substring(0, dash);
            }

            if (code.StartsWith("zh"))
            {
                yield return "zh";
            }

            if (code != "en")
            {
                yield return "en";
            }
        }
    }
}
