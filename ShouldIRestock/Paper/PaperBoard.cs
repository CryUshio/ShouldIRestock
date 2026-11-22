using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ShouldIRestock
{
    public static class PaperBoard
    {
        private const int RowsPerPage = 13;
        private const float TitleSize = 12f;
        private const float TitleY = 8f;
        private const float RowTop = 30f;
        private const float RowStep = 20f;
        private const float RowHeight = 18f;
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
            int pages = Mathf.Max(1, Mathf.CeilToInt(shot.Rows.Count / (float)RowsPerPage));
            paper = ModHelper.CreatePaper(ModIds.Id, ModIds.BookId, "sticky_sheet");
            BookChrome.Apply(paper);
            for (int page = 0; page < pages; page++)
            {
                paper.AddPage(Text.F("page_name", "Page {0}", page + 1));
                paper.AddText(12f, TitleY, 224f, 20f, Text.L("title", "要不要进货 Inventory Category Stats"), "handwritten", "title_" + page, "left", TitleSize);
                int start = page * RowsPerPage;
                for (int slot = 0; slot < RowsPerPage; slot++)
                {
                    int rowIndex = start + slot;
                    if (rowIndex >= shot.Rows.Count)
                    {
                        break;
                    }

                    TagRow row = shot.Rows[rowIndex];
                    bool special = row.Id == ModIds.Stolen || row.Id == ModIds.Contraband;
                    AddRow(row.Id, RowTop + slot * RowStep, Label(row.Id, row.Name, TagCatalog.ColorOf(row.Id), row.Count, row.Stolen, row.Contraband, !special));
                }
            }

            paper.Closed += OnClosed;
            paper.PageShown += OnPageShown;
            paper.Show();
            Bind();
            BookChrome.ClearSelection();
        }

        private static void AddRow(string id, float y, string label)
        {
            string rowId = "row_" + id;
            paper.AddButton(22f, y, 208f, RowHeight, label, () => TagHighlighter.ToggleLock(id), rowId);
        }

        private static void Write(StockSnapshot shot)
        {
            for (int i = 0; i < shot.Rows.Count; i++)
            {
                TagRow row = shot.Rows[i];
                bool special = row.Id == ModIds.Stolen || row.Id == ModIds.Contraband;
                Set("row_" + row.Id, Label(row.Id, row.Name, TagCatalog.ColorOf(row.Id), row.Count, row.Stolen, row.Contraband, !special));
            }

            Bind();
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

        private static string Label(string id, string name, Color color, int count, int stolen, int contraband, bool details)
        {
            string hex = ColorUtility.ToHtmlStringRGB(color);
            string text = "<color=#" + hex + ">[" + name + "]</color> (" + count + ")";
            if (!details)
            {
                return text;
            }

            if (stolen > 0)
            {
                text += "  " + Text.F("stolen_n", "stolen {0}", stolen);
            }

            if (contraband > 0)
            {
                text += "  " + Text.F("contraband_n", "contraband {0}", contraband);
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

                BookChrome.Style(paper, binding);
                if (!binding.data.id.StartsWith("row_"))
                {
                    continue;
                }

                if (binding.image != null)
                {
                    binding.image.color = Color.clear;
                }

                if (binding.text != null)
                {
                    binding.text.richText = true;
                    binding.text.color = new Color(0.2f, 0.14f, 0.05f, 1f);
                    binding.text.alignment = TextAlignmentOptions.MidlineLeft;
                    binding.text.margin = new Vector4(8f, 0f, 24f, 0f);
                }

                SelectionMark.Apply(binding, TagHighlighter.Locked == binding.data.id.Substring(4));
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
