using System.Collections.Generic;
using UnityEngine;

namespace ShouldIRestock
{
    public sealed class TagInfo
    {
        public string Id;
        public string Fallback;
        public Color Color;

        public string Name => Text.L("tag_" + Id, Fallback);
    }

    public static class TagCatalog
    {
        private static readonly TagInfo[] All = Build();
        private static readonly Dictionary<string, TagInfo> ById = Index();

        public static IReadOnlyList<TagInfo> Tags => All;

        public static TagInfo Find(string id)
        {
            TagInfo info;
            return id != null && ById.TryGetValue(id, out info) ? info : null;
        }

        public static Color ColorOf(string id)
        {
            if (id == ModIds.Stolen)
            {
                return new Color(1f, 0.7f, 0.1f, 0.95f);
            }

            if (id == ModIds.Contraband)
            {
                return new Color(0.95f, 0.22f, 0.2f, 0.95f);
            }

            TagInfo info = Find(id);
            return info != null ? info.Color : new Color(0.75f, 0.75f, 0.75f, 0.95f);
        }

        public static List<string> Match(GameItem item)
        {
            var hits = new List<string>(4);
            if (item == null)
            {
                return hits;
            }

            for (int i = 0; i < All.Length; i++)
            {
                if (Has(item, All[i].Id))
                {
                    hits.Add(All[i].Id);
                }
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

        private static Dictionary<string, TagInfo> Index()
        {
            var map = new Dictionary<string, TagInfo>(All.Length);
            for (int i = 0; i < All.Length; i++)
            {
                map[All[i].Id] = All[i];
            }

            return map;
        }

        private static TagInfo[] Build()
        {
            string[] ids =
            {
                "FOOD_ITEM_TAG", "FRESH_FOOD_TAG", "CANNED_FOOD_TAG", "SNACK_TAG", "MEAT_FOOD_TAG", "BERRY_TAG",
                "SEED_TAG", "SEED_ITEM_TAG", "ANIMAL_ITEM_TAG", "FISH_ITEM_TAG", "CHEM_ITEM_TAG", "DRUG_TAG",
                "MEDICAL_ITEM_TAG", "MEDICAL_SUPPLY_TAG", "FUEL_ITEM_TAG", "EXOTIC_FUEL_TAG", "PLASMA_CELL_TAG",
                "ENERGY_CELL", "MELEE_WEAPON_TAG", "SERVICE_GUN_TAG", "ANTIQUE_FIREARM_TAG", "GUN_FRAME_TAG",
                "ARMOR_ITEM_TAG", "AMMO_BOX_TAG", "BOOK_TAG", "CASSETTE_TAG", "GEMSTONE_TAG", "ELECTRONIC_PART_TAG",
                "CONSTRUCTION_MATERIAL_TAG", "WINE_INGREDIENT_TAG", "ALCOHOL_TAG", "CIGARETTE_TAG",
                "WATER_BOTTLE_ITEM_TAG", "ORGAN_ITEM_TAG", "NAV_DISK_TAG", "PERMIT_ITEM_TAG", "AMENITY_ITEM_TAG",
                "MACHINERY_TAG", "TOOLBOX_TAG", "PRINTER_CHIP_TAG", "MODULE", "NODE", "SALVAGABLE_ITEM_TAG"
            };
            string[] names =
            {
                "食物", "生鲜", "罐头", "零食", "肉类", "浆果",
                "种子", "种子物品", "动物", "鱼", "化学品", "药物",
                "医疗", "医疗物资", "燃料", "异种燃料", "等离子电池",
                "能量电池", "近战武器", "制式枪", "古董枪", "枪架",
                "护甲", "弹药", "书籍", "磁带", "宝石", "电子零件",
                "建材", "酿酒原料", "酒", "烟",
                "水瓶", "器官", "导航盘", "许可证", "生活用品",
                "机器", "工具箱", "芯片", "模块", "节点", "可回收"
            };
            var list = new TagInfo[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                Color color = Color.HSVToRGB((i * 0.618034f) % 1f, 0.62f, 0.95f);
                color.a = 0.95f;
                list[i] = new TagInfo { Id = ids[i], Fallback = names[i], Color = color };
            }

            return list;
        }
    }
}
