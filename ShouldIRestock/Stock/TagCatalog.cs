using System.Collections.Generic;
using UnityEngine;

namespace ShouldIRestock
{
    public static class TagCatalog
    {
        private const int Unknown = 1000;

        // 物品描边颜色：普通青色（cyan），赃物标准橙（#FFA500），违禁品红。
        public static Color OutlineColor(HighlightStyle style)
        {
            switch (style)
            {
                case HighlightStyle.Contraband: return new Color(0.95f, 0.22f, 0.2f, 0.95f);
                case HighlightStyle.Stolen: return new Color(1f, 0.64705884f, 0f, 0.95f);
                default: return new Color(0f, 1f, 1f, 0.95f);
            }
        }

        // 便签行的字色：赃物用标准橙，违禁品用品红，其余标签一律蓝色。
        public static Color ColorOf(string id)
        {
            if (id == ModIds.Stolen) return OutlineColor(HighlightStyle.Stolen);
            if (id == ModIds.Contraband) return OutlineColor(HighlightStyle.Contraband);
            return new Color(0.15f, 0.4f, 0.85f, 1f);
        }
        public static int OrderOf(string id)
        {
            int major = TypeHelper.MAJOR_TYPES_LIST.IndexOf(id);
            if (major >= 0)
            {
                return major;
            }

            int index = TypeHelper.ALL_TYPES_LIST.IndexOf(id);
            return index >= 0 ? TypeHelper.MAJOR_TYPES_LIST.Count + index : Unknown;
        }

        public static string NameOf(string id)
        {
            try
            {
                string name = TypeHelper.GetTypeDisplayName(id);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            catch
            {
            }

            return id;
        }

        // 读取游戏自己给物品标的类型（提示框里方括号里那些）。
        public static List<string> Match(GameItem item)
        {
            var hits = new List<string>(4);
            if (item == null)
            {
                return hits;
            }

            try
            {
                List<string> types = item.GetGameItemType();
                if (types == null)
                {
                    return hits;
                }

                bool fermenting = IsWineInProgress(item);
                for (int i = 0; i < types.Count; i++)
                {
                    string type = types[i];
                    if (fermenting && type == "HOUSEHOLD_GOOD")
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(type) && !hits.Contains(type))
                    {
                        hits.Add(type);
                    }
                }
            }
            catch
            {
            }

            return hits;
        }

        // 开始酿造后游戏会拿掉日用品，但读档时工厂又会把它加回类型列表。
        public static bool IsWineInProgress(GameItem item)
        {
            try
            {
                if (item == null || !item.IsTag("WINE_CONTAINER_TAG"))
                {
                    return false;
                }

                return item.GetTagReadonly("WINE_CONTAINER_STATE").GetInt() != 0;
            }
            catch
            {
                return false;
            }
        }

        public static bool Has(GameItem item, string tag)
        {
            try
            {
                return item != null && item.IsTag(tag);
            }
            catch
            {
                return false;
            }
        }
    }
}