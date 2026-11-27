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
        private static readonly Dictionary<string, string> values = new Dictionary<string, string>();

        public static void Sync(StockSnapshot shot)
        {
            if (paper == null || !paper.IsAlive || shot == null)
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

        public static void Place()
        {
            PaperPlace.Tick(paper);
        }

        public static void Close()
        {
            TagHighlighter.Release();
            Tear();
        }

        private static void Tear()
        {
            PaperPlace.Capture(paper);
            TagHighlighter.ClearHover();
            PaperUI ui = paper;
            paper = null;
            signature = "";
            values.Clear();
            try
            {
                if (ui != null && ui.IsAlive)
                {
                    ui.Close();
                }
                else
                {
                    ModHelper.ClosePaper(ModIds.Id, ModIds.BookId);
                }
            }
            catch
            {
            }
        }

        private static void Open(StockSnapshot shot)
        {
            Tear();
            signature = shot.Signature;
            int pages = Mathf.Max(1, Mathf.CeilToInt(shot.Rows.Count / (float)RowsPerPage));
            paper = ModHelper.CreatePaper(ModIds.Id, ModIds.BookId, "sticky_sheet");
            BookChrome.Apply(paper);
            for (int page = 0; page < pages; page++)
            {
                paper.AddPage(Text.F("page_name", "Page {0}", page + 1));
                paper.AddText(12f, TitleY, 168f, 20f, Text.L("title", "要不要进货 Inventory Category Stats"), "handwritten", "title_" + page, "left", TitleSize);
                paper.AddIconButton(187f, 8f, 14f, 15f, "paper_buttons_wrench", ItemExcludeWindow.Open, "exclude_" + page, "exclude");
                int start = page * RowsPerPage;
                for (int slot = 0; slot < RowsPerPage; slot++)
                {
                    int rowIndex = start + slot;
                    if (rowIndex >= shot.Rows.Count)
                    {
                        break;
                    }

                    TagRow row = shot.Rows[rowIndex];
                    AddRow(row.Id, RowTop + slot * RowStep, Label(row));
                }
            }

            paper.Closed += OnClosed;
            paper.PageShown += OnPageShown;
            paper.Show();
            BookChrome.RememberZoom(paper);
            PaperPlace.Arm();
            Bind();
            WriteValues(shot);
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
                Set("row_" + row.Id, Label(row));
            }

            Bind();
            WriteValues(shot);
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
                if (binding == null || binding.data == null || binding.data.id != id)
                {
                    continue;
                }

                binding.data.text = label;
                if (binding.text == null)
                {
                    continue;
                }

                binding.text.text = label;
                MoneyFormat.Apply(binding.text);
                binding.text.ForceMeshUpdate();
            }
        }

        private static string Label(TagRow row)
        {
            string name = ColorUtility.ToHtmlStringRGB(TagCatalog.ColorOf(row.Id));
            string title = "<color=#" + name + ">[" + row.Name + "]</color>";
            if (row.Id == ModIds.Stolen)
            {
                return title + Count(row.Count, TagCatalog.OutlineColor(HighlightStyle.Stolen));
            }

            if (row.Id == ModIds.Contraband)
            {
                return title + Count(row.Count, TagCatalog.OutlineColor(HighlightStyle.Contraband));
            }

            string stolen = ColorUtility.ToHtmlStringRGB(TagCatalog.OutlineColor(HighlightStyle.Stolen));
            string contraband = ColorUtility.ToHtmlStringRGB(TagCatalog.OutlineColor(HighlightStyle.Contraband));
            return title + "(" + row.Normal
                + "/<color=#" + stolen + ">" + row.Stolen + "</color>"
                + "/<color=#" + contraband + ">" + row.Contraband + "</color>)";
        }

        private static string Count(int count, Color color)
        {
            return "(<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + count + "</color>)";
        }

        private static void WriteValues(StockSnapshot shot)
        {
            values.Clear();
            if (shot != null)
            {
                for (int i = 0; i < shot.Rows.Count; i++)
                {
                    TagRow row = shot.Rows[i];
                    values[row.Id] = MoneyFormat.Text(row.Value);
                }
            }

            PaintValues();
        }

        private static void PaintValues()
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
                if (binding == null || binding.data == null || string.IsNullOrEmpty(binding.data.id) || !binding.data.id.StartsWith("row_"))
                {
                    continue;
                }

                string money;
                if (!values.TryGetValue(binding.data.id.Substring(4), out money))
                {
                    continue;
                }

                RowMoney.Apply(binding, money);
            }
        }

        private static void OnPageShown(PaperUI ui, int page)
        {
            Bind();
            PaintValues();
        }

        private static void OnClosed(PaperUI ui)
        {
            if (paper != ui)
            {
                return;
            }

            if (ui != null && ui.IsAlive)
            {
                return;
            }

            TagHighlighter.Release();
            paper = null;
            signature = "";
            values.Clear();
        }

        public static void RefreshMarks()
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
                if (binding == null || binding.go == null || binding.data == null || string.IsNullOrEmpty(binding.data.id) || !binding.data.id.StartsWith("row_"))
                {
                    continue;
                }

                SelectionMark.Apply(binding, Marked(binding.data.id.Substring(4)));
            }
        }

        private static bool Marked(string tagId)
        {
            return tagId == TagHighlighter.Locked || tagId == TagHighlighter.Hover;
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
                    binding.text.margin = new Vector4(8f, 0f, 76f, 0f);
                    MoneyFormat.Apply(binding.text);
                }

                SelectionMark.Apply(binding, Marked(binding.data.id.Substring(4)));
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
