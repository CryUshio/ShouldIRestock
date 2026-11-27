using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ShouldIRestock
{
    public static class GeneralOptions
    {
        private const string FileName = "general.txt";
        private static bool loaded;
        private static bool ignoreLocked = true;
        private static bool skipDebris = true;
        private static bool countExcludedLegality = true;
        private static int thickness = 1;
        private static float scanSeconds = 0.4f;
        private static bool noteOnRight;

        public static bool IgnoreLocked
        {
            get
            {
                Load();
                return ignoreLocked;
            }
        }

        public static bool SkipDebris
        {
            get
            {
                Load();
                return skipDebris;
            }
        }

        public static bool CountExcludedLegality
        {
            get
            {
                Load();
                return countExcludedLegality;
            }
        }

        public static int Thickness
        {
            get
            {
                Load();
                return thickness;
            }
        }

        public static float ScanSeconds
        {
            get
            {
                Load();
                return scanSeconds;
            }
        }

        public static bool NoteOnRight
        {
            get
            {
                Load();
                return noteOnRight;
            }
        }

        public static void SetIgnoreLocked(bool value)
        {
            Load();
            if (ignoreLocked == value)
            {
                return;
            }

            ignoreLocked = value;
            Commit();
        }

        public static void SetSkipDebris(bool value)
        {
            Load();
            if (skipDebris == value)
            {
                return;
            }

            skipDebris = value;
            Commit();
        }

        public static void SetCountExcludedLegality(bool value)
        {
            Load();
            if (countExcludedLegality == value)
            {
                return;
            }

            countExcludedLegality = value;
            Commit();
        }

        public static void SetThickness(int value)
        {
            Load();
            value = Mathf.Clamp(value, 1, 6);
            if (thickness == value)
            {
                return;
            }

            thickness = value;
            Commit();
        }

        public static void SetScanSeconds(float value)
        {
            Load();
            value = Mathf.Clamp(value, 0.2f, 2f);
            if (Mathf.Abs(scanSeconds - value) < 0.001f)
            {
                return;
            }

            scanSeconds = value;
            Commit();
        }

        public static void SetNoteOnRight(bool value)
        {
            Load();
            if (noteOnRight == value)
            {
                return;
            }

            noteOnRight = value;
            Commit();
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

            if (text == null)
            {
                ImportFromConfig();
                Save();
                return;
            }

            ApplyLines(text);
        }

        private static void Commit()
        {
            Save();
            StoreDriver.Wake();
        }

        private static void ApplyLines(string text)
        {
            string[] lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                int split = lines[i].IndexOf('=');
                if (split <= 0)
                {
                    continue;
                }

                string key = lines[i].Substring(0, split).Trim();
                string value = lines[i].Substring(split + 1).Trim();
                Apply(key, value);
            }
        }

        private static void Apply(string key, string value)
        {
            switch (key)
            {
                case "ignore_locked":
                    ignoreLocked = value == "true";
                    break;
                case "skip_debris":
                    skipDebris = value == "true";
                    break;
                case "count_excluded_legality":
                    countExcludedLegality = value == "true";
                    break;
                case "thickness":
                    int thick;
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out thick))
                    {
                        thickness = Mathf.Clamp(thick, 1, 6);
                    }
                    break;
                case "scan_seconds":
                    float seconds;
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
                    {
                        scanSeconds = Mathf.Clamp(seconds, 0.2f, 2f);
                    }
                    break;
                case "note_side":
                    noteOnRight = value == "right";
                    break;
            }
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
                "\"key\"\\s*:\\s*\"(ignore_locked|skip_debris|count_excluded_legality|thickness|scan_seconds|note_side)\"\\s*,\\s*\"value\"\\s*:\\s*\"([^\"]*)\"",
                RegexOptions.IgnoreCase);
            for (int i = 0; i < matches.Count; i++)
            {
                Apply(matches[i].Groups[1].Value, matches[i].Groups[2].Value);
            }
        }

        private static void Save()
        {
            var lines = new List<string>
            {
                "ignore_locked=" + (ignoreLocked ? "true" : "false"),
                "skip_debris=" + (skipDebris ? "true" : "false"),
                "count_excluded_legality=" + (countExcludedLegality ? "true" : "false"),
                "thickness=" + thickness.ToString(CultureInfo.InvariantCulture),
                "scan_seconds=" + scanSeconds.ToString("0.###", CultureInfo.InvariantCulture),
                "note_side=" + (noteOnRight ? "right" : "left")
            };
            try
            {
                ModHelper.WriteModDataFileText(ModIds.Id, FileName, string.Join("\n", lines));
            }
            catch
            {
            }
        }
    }
}
