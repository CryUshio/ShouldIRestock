using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ShouldIRestock
{
    public static class DetailExclude
    {
        private const string FileName = "excluded-tags.txt";
        private static readonly HashSet<string> Selected = new HashSet<string>();
        private static readonly List<string> Ids = new List<string>();
        private static bool loaded;
        private static bool listed;

        public static bool Contains(string id)
        {
            Load();
            return id != null && Selected.Contains(id);
        }

        public static void Set(string id, bool on)
        {
            Load();
            if (!IsDetail(id))
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

        public static List<string> IdsInOrder()
        {
            EnsureIds();
            return Ids;
        }

        public static void Load()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            EnsureIds();
            string text = null;
            try
            {
                text = ModHelper.ReadModDataFileText(ModIds.Id, FileName);
            }
            catch
            {
                text = null;
            }

            if (text == null)
            {
                ImportFromConfig();
                Save();
                return;
            }

            string[] lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string id = lines[i].Trim();
                if (IsDetail(id))
                {
                    Selected.Add(id);
                }
            }
        }

        private static void EnsureIds()
        {
            if (listed)
            {
                return;
            }

            listed = true;
            var majors = new HashSet<string>(TypeHelper.MAJOR_TYPES_LIST);
            List<string> all = TypeHelper.ALL_TYPES_LIST;
            for (int i = 0; i < all.Count; i++)
            {
                string id = all[i];
                if (id == null || id == ModIds.ContrabandType || majors.Contains(id) || Ids.Contains(id))
                {
                    continue;
                }

                Ids.Add(id);
            }
        }

        private static bool IsDetail(string id)
        {
            EnsureIds();
            return id != null && Ids.Contains(id);
        }

        private static void ImportFromConfig()
        {
            string path = Path.Combine(ModConfig.RootFolder, ModIds.Id + ".json");
            if (!File.Exists(path))
            {
                return;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch
            {
                return;
            }

            MatchCollection matches = Regex.Matches(
                json,
                "\"key\"\\s*:\\s*\"exclude_([^\"]+)\"\\s*,\\s*\"value\"\\s*:\\s*\"true\"",
                RegexOptions.IgnoreCase);
            for (int i = 0; i < matches.Count; i++)
            {
                string id = matches[i].Groups[1].Value;
                if (IsDetail(id))
                {
                    Selected.Add(id);
                }
            }
        }

        private static void Save()
        {
            var ids = new List<string>(Selected);
            ids.Sort(System.StringComparer.Ordinal);
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
