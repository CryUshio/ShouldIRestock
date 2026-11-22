using System.Collections.Generic;
using UnityEngine;

namespace ShouldIRestock
{
    public static class TagCatalog
    {
        private const int Unknown = 1000;

        // 物品描边颜色，沿用 hollow.clientdemandhighlight：普通绿、赃物琥珀、违禁品红。
        public static Color OutlineColor(HighlightStyle style)
        {
            switch (style)
            {
                case HighlightStyle.Contraband: return new Color(0.95f, 0.22f, 0.2f, 0.95f);
                case HighlightStyle.Stolen: return new Color(1f, 0.7f, 0.1f, 0.95f);
                default: return new Color(0.3f, 0.9f, 0.35f, 0.95f);
            }
        }

        // 便签行的字色：赃物、违禁品保持原来的琥珀和红，其余标签一律蓝色。
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

                for (int i = 0; i < types.Count; i++)
                {
                    string type = types[i];
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