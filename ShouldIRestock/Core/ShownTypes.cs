using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ShouldIRestock
{
    public static class ShownTypes
    {
        private const string FileName = "shown-types.txt";
        private static readonly HashSet<string> Shown = new HashSet<string>();
        private static readonly List<string> Ids = new List<string>();
        private static bool loaded;
        private static bool listed;

        public static bool Shows(string id)
        {
            Load();
            return id != null && Shown.Contains(id);
        }

        public static void Set(string id, bool on)
        {
            Load();
            if (!IsKnown(id))
            {
                return;
            }

            if (on)
            {
                Shown.Add(id);
            }
            else
            {
                Shown.Remove(id);
            }

            Save();
            StoreDriver.Wake();
        }

        public static List<string> IdsInOrder()
        {
            EnsureIds();
            return Ids;
        }

        public static string NameOf(string id)
        {
            if (id == ModIds.Stolen)
            {
                return Text.L("all_stolen", "All stolen");
            }

            if (id == ModIds.Contraband)
            {
                return Text.L("all_contraband", "All contraband");
            }

            if (id == ModIds.Other)
            {
                return Text.L("tag_other", "Uncategorized");
            }

            return TagCatalog.NameOf(id);
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
                if (IsKnown(id))
                {
                    Shown.Add(id);
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
            Ids.Add(ModIds.Stolen);
            Ids.Add(ModIds.Contraband);
            AddAll(TypeHelper.MAJOR_TYPES_LIST);
            AddAll(TypeHelper.ALL_TYPES_LIST);
            Ids.Add(ModIds.Other);
        }

        private static void AddAll(List<string> source)
        {
            for (int i = 0; i < source.Count; i++)
            {
                string id = source[i];
                if (id == null || id == ModIds.ContrabandType || Ids.Contains(id))
                {
                    continue;
                }

                Ids.Add(id);
            }
        }

        private static bool IsKnown(string id)
        {
            EnsureIds();
            return id != null && Ids.Contains(id);
        }

        private static bool DefaultOn(string id)
        {
            return id == ModIds.Stolen || id == ModIds.Contraband || TypeHelper.MAJOR_TYPES_LIST.Contains(id);
        }

        private static void ApplyDefaults()
        {
            for (int i = 0; i < Ids.Count; i++)
            {
                if (DefaultOn(Ids[i]))
                {
                    Shown.Add(Ids[i]);
                }
            }
        }

        private static void ImportFromConfig()
        {
            string path = Path.Combine(ModConfig.RootFolder, ModIds.Id + ".json");
            string json = null;
            if (File.Exists(path))
            {
                try
                {
                    json = File.ReadAllText(path);
                }
                catch
                {
                    json = null;
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                ApplyDefaults();
                return;
            }

            MatchCollection matches = Regex.Matches(
                json,
                "\"key\"\\s*:\\s*\"show_([^\"]+)\"\\s*,\\s*\"value\"\\s*:\\s*\"(true|false)\"",
                RegexOptions.IgnoreCase);
            if (matches.Count == 0)
            {
                ApplyDefaults();
                return;
            }

            var found = new Dictionary<string, bool>();
            for (int i = 0; i < matches.Count; i++)
            {
                found[matches[i].Groups[1].Value] = matches[i].Groups[2].Value.Equals("true", System.StringComparison.OrdinalIgnoreCase);
            }

            for (int i = 0; i < Ids.Count; i++)
            {
                string id = Ids[i];
                bool on;
                if (!found.TryGetValue(id, out on))
                {
                    on = DefaultOn(id);
                }

                if (on)
                {
                    Shown.Add(id);
                }
            }
        }

        private static void Save()
        {
            var ids = new List<string>(Shown);
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
