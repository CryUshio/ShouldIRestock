using System;
using System.Collections.Generic;

namespace ShouldIRestock
{
    public static class ItemExclude
    {
        private const string FileName = "excluded-items.txt";
        private const string SeedFile = "excluded-seed.txt";
        private static readonly string[] Defaults = { "water_tablet" };
        private static readonly HashSet<string> Selected = new HashSet<string>();
        private static readonly List<Named> Catalog = new List<Named>();
        private static bool loaded;
        private static bool catalogReady;

        public sealed class Named
        {
            public string Id;
            public string Name;
        }

        public static bool Contains(string id)
        {
            Load();
            return id != null && Selected.Contains(id);
        }

        // 发酵会换贴图、拿掉品类，酿完还会改显示名，但还是同一个酒瓶。
        public static bool Matches(GameItem item)
        {
            if (item == null)
            {
                return false;
            }

            if (Contains(item.identifier))
            {
                return true;
            }

            if (!Contains("wine_bottle"))
            {
                return false;
            }

            if (TagCatalog.Has(item, "WINE_CONTAINER_TAG"))
            {
                return true;
            }

            string sprite = null;
            string atlas = null;
            try
            {
                sprite = item.spritePath;
                atlas = item.spriteAtlasPath;
            }
            catch
            {
                sprite = null;
            }

            if (sprite == "wine_bottle" || sprite == "wine_fermenting")
            {
                return true;
            }

            return atlas == "Items/items_winemaking" && sprite != null && sprite.StartsWith("bottle");
        }

        public static void Set(string id, bool on)
        {
            Load();
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            if (on)
            {
                Selected.Add(id);
            }
            else
            {
                Selected.Remove(id);
            }

            Save();
            StoreDriver.Wake();
        }

        public static void Load()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            string text = null;
            try
            {
                text = ModHelper.ReadModDataFileText(ModIds.Id, FileName);
            }
            catch
            {
                text = null;
            }

            if (!string.IsNullOrEmpty(text))
            {
                string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string id = lines[i].Trim();
                    if (id.Length > 0)
                    {
                        Selected.Add(id);
                    }
                }
            }

            ApplyNewDefaults();
        }

        // 新的默认排除只补一次。玩家之后取消勾选不会在下次启动时被加回来。
        private static void ApplyNewDefaults()
        {
            var seeded = ReadIds(SeedFile);
            bool selectionChanged = false;
            bool seedChanged = false;
            for (int i = 0; i < Defaults.Length; i++)
            {
                string id = Defaults[i];
                if (!seeded.Add(id))
                {
                    continue;
                }

                seedChanged = true;
                if (Selected.Add(id))
                {
                    selectionChanged = true;
                }
            }

            if (selectionChanged)
            {
                Save();
            }

            if (seedChanged)
            {
                WriteIds(SeedFile, seeded);
            }
        }

        private static HashSet<string> ReadIds(string file)
        {
            var ids = new HashSet<string>();
            string text = null;
            try
            {
                text = ModHelper.ReadModDataFileText(ModIds.Id, file);
            }
            catch
            {
                text = null;
            }

            if (string.IsNullOrEmpty(text))
            {
                return ids;
            }

            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string id = lines[i].Trim();
                if (id.Length > 0)
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private static void WriteIds(string file, HashSet<string> ids)
        {
            var list = new List<string>(ids);
            list.Sort(StringComparer.Ordinal);
            try
            {
                ModHelper.WriteModDataFileText(ModIds.Id, file, string.Join("\n", list));
            }
            catch
            {
            }
        }

        public static void EnsureCatalog()
        {
            if (catalogReady)
            {
                return;
            }

            catalogReady = true;
            List<string> ids = null;
            try
            {
                ids = DirectoryMaster.GetIdentifierList<GameItem>();
            }
            catch
            {
                ids = null;
            }

            if (ids == null)
            {
                return;
            }

            var seen = new HashSet<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                {
                    continue;
                }

                Catalog.Add(new Named { Id = id, Name = ResolveName(id) });
            }

            Catalog.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        public static List<Named> Filter(string query, int limit, out bool more)
        {
            Load();
            EnsureCatalog();
            more = false;
            var result = new List<Named>();
            string q = query == null ? "" : query.Trim();
            if (q.Length == 0)
            {
                var seen = new HashSet<string>();
                for (int i = 0; i < Catalog.Count; i++)
                {
                    Named row = Catalog[i];
                    if (!Selected.Contains(row.Id) || !seen.Add(row.Id))
                    {
                        continue;
                    }

                    result.Add(row);
                }

                var leftovers = new List<string>();
                foreach (string id in Selected)
                {
                    if (seen.Add(id))
                    {
                        leftovers.Add(id);
                    }
                }

                leftovers.Sort(StringComparer.Ordinal);
                for (int i = 0; i < leftovers.Count; i++)
                {
                    result.Add(new Named { Id = leftovers[i], Name = leftovers[i] });
                }

                return result;
            }

            for (int i = 0; i < Catalog.Count; i++)
            {
                Named row = Catalog[i];
                if (row.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                    && row.Id.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (result.Count >= limit)
                {
                    more = true;
                    break;
                }

                result.Add(row);
            }

            return result;
        }

        private static string ResolveName(string id)
        {
            GameItem item = null;
            try
            {
                item = DirectoryMaster.Create<GameItem>(id);
            }
            catch
            {
                item = null;
            }

            string name = null;
            try
            {
                if (item != null)
                {
                    name = item.GetDisplayName();
                }
            }
            catch
            {
                name = null;
            }

            try
            {
                if (item != null)
                {
                    item.Destroy();
                }
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(name))
            {
                return id;
            }

            return name;
        }

        private static void Save()
        {
            var ids = new List<string>(Selected);
            ids.Sort(StringComparer.Ordinal);
            try
            {
                ModHelper.WriteModDataFileText(ModIds.Id, FileName, string.Join("\n", ids));
            }
            catch
            {
            }
        }
    }
}
