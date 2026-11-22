using System.Collections.Generic;
using UnityEngine;

namespace ShouldIRestock
{
    public static class PaperBoard
    {
        private const int RowsPerPage = 10;
        private static PaperUI paper;
        private static string signature = "";

        public static void Sync(StockSnapshot shot)
        {
            if (paper == null || !paper.IsAlive || !paper.IsVisible || shot == null)
            {
                return;
            }

            if (signature != shot.Signature)
            {
                Open(shot);
                return;
            }

            Write(shot);
        }

        public static void Toggle(StockSnapshot shot)
        {
            if (paper != null && paper.IsAlive && paper.IsVisible)
            {
                Close();
                return;
            }

            Open(shot ?? StockScanner.Scan());
        }

        public static void Close()
        {
            TagHighlighter.ClearHover();
            try
            {
                if (paper != null && paper.IsAlive)
                {
                    paper.Close();
                }
                else
                {
                    ModHelper.ClosePaper(ModIds.Id, ModIds.BookId);
                }
            }
            catch
            {
            }

            paper = null;
            signature = "";
        }

        private static void Open(StockSnapshot shot)
        {
            Close();
            signature = shot.Signature;
            int pages = Mathf.Max(1, Mathf.CeilToInt((shot.Rows.Count + 2) / (float)RowsPerPage));
            paper = ModHelper.CreatePaper(ModIds.Id, ModIds.BookId, "sticky_sheet");
            for (int page = 0; page < pages; page++)
            {
                paper.AddPage(Text.F("page_name", "第 {0} 页", page + 1));
                paper.AddText(16f, 10f, 216f, 18f, Text.L("title", "要不要进货"), "handwritten", "title_" + page, "left", 11f);
                int reserved = page == 0 ? 2 : 0;
                int start = page == 0 ? 0 : page * RowsPerPage - 2;
                for (int slot = 0; slot < RowsPerPage; slot++)
                {
                    float y = 34f + slot * 22f;
                    if (page == 0 && slot == 0)
                    {
                        AddRow(ModIds.Stolen, y, Label(Text.L("all_stolen", "全部赃物"), TagCatalog.ColorOf(ModIds.Stolen), shot.Stolen, 0, 0, false));
                        continue;
                    }

                    if (page == 0 && slot == 1)
                    {
                        AddRow(ModIds.Contraband, y, Label(Text.L("all_contraband", "全部违禁品"), TagCatalog.ColorOf(ModIds.Contraband), shot.Contraband, 0, 0, false));
                        continue;
                    }

                    int rowIndex = start + slot - reserved;
                    if (rowIndex < 0 || rowIndex >= shot.Rows.Count)
                    {
                        continue;
                    }

                    TagRow row = shot.Rows[rowIndex];
                    AddRow(row.Id, y, Label(row.Name, TagCatalog.ColorOf(row.Id), row.Count, row.Stolen, row.Contraband, true));
                }
            }

            paper.Closed += OnClosed;
            paper.PageShown += OnPageShown;
            paper.Show();
            Bind();
        }

        private static void AddRow(string id, float y, string label)
        {
            string rowId = "row_" + id;
            paper.AddButton(14f, y, 220f, 20f, label, () => TagHighlighter.ToggleLock(id), rowId);
        }

        private static void Write(StockSnapshot shot)
        {
            Set("row_" + ModIds.Stolen, Label(Text.L("all_stolen", "全部赃物"), TagCatalog.ColorOf(ModIds.Stolen), shot.Stolen, 0, 0, false));
            Set("row_" + ModIds.Contraband, Label(Text.L("all_contraband", "全部违禁品"), TagCatalog.ColorOf(ModIds.Contraband), shot.Contraband, 0, 0, false));
            for (int i = 0; i < shot.Rows.Count; i++)
            {
                TagRow row = shot.Rows[i];
                Set("row_" + row.Id, Label(row.Name, TagCatalog.ColorOf(row.Id), row.Count, row.Stolen, row.Contraband, true));
            }
        }

        private static void Set(string id, string label)
        {
            try
            {
                paper.SetText(id, label);
            }
            catch
            {
            }
        }

        private static string Label(string name, Color color, int count, int stolen, int contraband, bool details)
        {
            string hex = ColorUtility.ToHtmlStringRGB(color);
            string text = "<color=#" + hex + ">[" + name + "]</color> (" + count + ")";
            if (!details)
            {
                return text;
            }

            if (stolen > 0)
            {
                text += "  " + Text.F("stolen_n", "赃 {0}", stolen);
            }

            if (contraband > 0)
            {
                text += "  " + Text.F("contraband_n", "违 {0}", contraband);
            }

            return text;
        }

        private static void OnPageShown(PaperUI ui, int page)
        {
            Bind();
        }

        private static void OnClosed(PaperUI ui)
        {
            TagHighlighter.ClearHover();
            if (paper == ui)
            {
                paper = null;
                signature = "";
            }
        }

        private static void Bind()
        {
            if (paper == null)
            {
                return;
            }

            List<PaperUI.Binding> bindings = null;
            try
            {
                bindings = paper.VisibleBindings();
            }
            catch
            {
                return;
            }

            if (bindings == null)
            {
                return;
            }

            for (int i = 0; i < bindings.Count; i++)
            {
                PaperUI.Binding binding = bindings[i];
                if (binding == null || binding.go == null || binding.data == null || string.IsNullOrEmpty(binding.data.id))
                {
                    continue;
                }

                if (!binding.data.id.StartsWith("row_"))
                {
                    continue;
                }

                if (binding.image != null)
                {
                    binding.image.color = new Color(1f, 0.96f, 0.75f, 0.2f);
                }

                if (binding.text != null)
                {
                    binding.text.richText = true;
                    binding.text.color = new Color(0.2f, 0.14f, 0.05f, 1f);
                }

                RowHover hover = binding.go.GetComponent<RowHover>();
                if (hover == null)
                {
                    hover = binding.go.AddComponent<RowHover>();
                }

                hover.TagId = binding.data.id.Substring(4);
            }
        }
    }
}
