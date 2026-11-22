using System.Collections.Generic;
using UnityEngine;

namespace ShouldIRestock
{
    public static class Settings
    {
        public static void Init()
        {
            GeneralOptions.Load();
        }

        public static bool IgnoreLocked => GeneralOptions.IgnoreLocked;

        public static bool SkipDebris => GeneralOptions.SkipDebris;

        public static bool KeepsLegality => GeneralOptions.CountExcludedLegality;

        public static bool IsExcluded(GameItem item, List<string> tags)
        {
            if (ItemExclude.Matches(item))
            {
                return true;
            }

            if (tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                if (ExcludesDetail(tags[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ExcludesDetail(string id)
        {
            return DetailExclude.Contains(id);
        }

        public static bool Shows(string id)
        {
            return ShownTypes.Shows(id);
        }

        public static int LineThickness => Mathf.Clamp(GeneralOptions.Thickness, 1, 6);

        public static float Interval => Mathf.Clamp(GeneralOptions.ScanSeconds, 0.2f, 2f);
    }
}
