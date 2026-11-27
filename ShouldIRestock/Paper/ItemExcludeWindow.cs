using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace ShouldIRestock
{
    public static class ItemExcludeWindow
    {
        private const int MatchLimit = 40;
        private const string WindowId = "should_i_restock:exclude_items";

        private static string query = "";
        private static bool filling;
        private static bool refreshWhenIdle;
        private static CustomUIWindow window;
        private static RectTransform listRoot;

        public static void Open()
        {
            CustomUIManager manager = CustomUIManager.Instance;
            if (manager == null)
            {
                return;
            }

            ItemExclude.Load();
            ItemExclude.EnsureCatalog();
            DetailExclude.Load();
            ShownTypes.Load();
            bool store = StoreUIManager.Instance != null;
            CustomUIBuilder builder = manager.CreateWindow(
                WindowId,
                Text.L("exclude_window", "Exclude"),
                store ? "meta" : "menu",
                manager.optionUIPrefab);
            if (builder == null || builder.GetWindow() == null)
            {
                return;
            }

            window = builder.GetWindow();
            builder.SetCloseOnEscape(!store);
            builder.SetSize(460f, 560f);
            builder.OnClose(Closed);
            builder.BeginTabs();
            builder.BeginTab(Text.L("tab_general", "General"));
            BeginPageScroll(builder);
            FillGeneral(builder);
            builder.End();
            builder.End();
            builder.BeginTab(Text.L("tab_show", "Show types"));
            builder.AddHeader(Text.L("opt_types", "Show these types"));
            BeginPageScroll(builder);
            FillShown(builder);
            builder.End();
            builder.End();
            builder.BeginTab(Text.L("tab_exclude_tags", "Exclude tags"));
            builder.AddHeader(Text.L("opt_exclude_types", "Exclude these detail tags"));
            BeginPageScroll(builder);
            FillTags(builder);
            builder.End();
            builder.End();
            builder.BeginTab(Text.L("tab_exclude_items", "Exclude items"));
            builder.AddInput(
                Text.L("exclude_search", "Search items"),
                query,
                null,
                "exclude_search",
                OnType);
            BeginPageScroll(builder);
            builder.AddLabel(" ", "exclude_anchor");
            builder.End();
            builder.End();
            builder.EndTabs();
            builder.Show();
            if (window.ContentRoot != null)
            {
                CustomUIBuilder.RebuildLayout(window.ContentRoot);
            }
            listRoot = FindListRoot();
            Fill();
        }

        public static void Tick()
        {
            if (!refreshWhenIdle || filling)
            {
                return;
            }

            if (window == null || !window.IsAlive)
            {
                refreshWhenIdle = false;
                return;
            }

            refreshWhenIdle = false;
            Fill();
        }

        private static void Closed()
        {
            window = null;
            listRoot = null;
            refreshWhenIdle = false;
        }

        private static void OnType(string text)
        {
            query = text ?? "";
            Fill();
        }

        private static RectTransform FindListRoot()
        {
            if (window == null)
            {
                return null;
            }

            CustomUIElement anchor = window.Get("exclude_anchor");
            if (anchor != null && anchor.Rect != null && anchor.Rect.parent is RectTransform parent)
            {
                return parent;
            }

            return null;
        }

        private static void BeginPageScroll(CustomUIBuilder builder)
        {
            Expand(builder.CurrentContainer);
            RectTransform pages = builder.CurrentContainer != null ? builder.CurrentContainer.parent as RectTransform : null;
            Expand(pages);
            Expand(pages != null ? pages.parent as RectTransform : null);
            builder.BeginScrollFlexible();
        }

        private static void Expand(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            LayoutElement layout = rect.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = rect.gameObject.AddComponent<LayoutElement>();
            }

            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 1f;
        }

        private static void FillGeneral(CustomUIBuilder builder)
        {
            float inner = WidenItemGap(builder);
            filling = true;
            try
            {
                builder.BeginColumn(inner);
                builder.AddLabel(Text.L("opt_note_side", "Note side"), "label_note_side");
                builder.AddDropdown(
                    new System.Collections.Generic.List<string>
                    {
                        Text.L("opt_note_left", "Left"),
                        Text.L("opt_note_right", "Right")
                    },
                    GeneralOptions.NoteOnRight ? 1 : 0,
                    index => GeneralChanged(() => GeneralOptions.SetNoteOnRight(index == 1)),
                    "opt_note_side");
                AddTip(builder, Text.L("opt_note_side_tip", "Left sits under the phone. Right sits to the right of the recorder."), "tip_note_side");
                builder.End();
                builder.BeginColumn(inner);
                builder.AddToggle(
                    Text.L("opt_ignore_locked", "Skip goods inside locked containers"),
                    GeneralOptions.IgnoreLocked,
                    on => GeneralChanged(() => GeneralOptions.SetIgnoreLocked(on)),
                    "opt_ignore_locked");
                AddTip(builder, Text.L("opt_ignore_locked_tip", "Locked boxes and items behind a lock are left out of the tally and the outline."), "tip_ignore_locked");
                builder.End();
                builder.BeginColumn(inner);
                builder.AddToggle(
                    Text.L("opt_skip_debris", "Skip debris"),
                    GeneralOptions.SkipDebris,
                    on => GeneralChanged(() => GeneralOptions.SetSkipDebris(on)),
                    "opt_skip_debris");
                AddTip(builder, Text.L("opt_skip_debris_tip", "Piles you cannot move are not counted as stock."), "tip_skip_debris");
                builder.End();
                builder.BeginColumn(inner);
                builder.AddToggle(
                    Text.L("opt_count_excluded", "Still count excluded goods as stolen and contraband"),
                    GeneralOptions.CountExcludedLegality,
                    on => GeneralChanged(() => GeneralOptions.SetCountExcludedLegality(on)),
                    "opt_count_excluded");
                AddTip(builder, Text.L("opt_count_excluded_tip", "When on, excluded goods still count toward All stolen and All contraband, and they outline when those rows are hovered."), "tip_count_excluded");
                builder.End();
                builder.BeginColumn(inner);
                builder.AddLabel(Text.L("opt_thickness", "Outline thickness"), "label_thickness");
                builder.AddSlider(1f, 6f, GeneralOptions.Thickness, value => GeneralChanged(() => GeneralOptions.SetThickness(Mathf.RoundToInt(value))), true, "opt_thickness");
                builder.End();
                builder.BeginColumn(inner);
                builder.AddLabel(ScanText(GeneralOptions.ScanSeconds), "label_scan");
                CustomUIElement scanLabel = builder.Last;
                builder.AddSlider(0.2f, 2f, GeneralOptions.ScanSeconds, value =>
                {
                    if (scanLabel != null)
                    {
                        scanLabel.SetText(ScanText(value));
                    }

                    GeneralChanged(() => GeneralOptions.SetScanSeconds(value));
                }, false, "opt_scan");
                builder.End();
            }
            finally
            {
                filling = false;
            }
        }

        private static float WidenItemGap(CustomUIBuilder builder)
        {
            float inner = 4f;
            VerticalLayoutGroup group = builder.CurrentContainer != null
                ? builder.CurrentContainer.GetComponent<VerticalLayoutGroup>()
                : null;
            if (group != null)
            {
                inner = group.spacing;
                group.spacing = inner + 4f;
            }

            return inner;
        }

        private static void AddTip(CustomUIBuilder builder, string text, string id)
        {
            builder.AddLabel(text, id);
            if (builder.Last == null || builder.Last.text == null)
            {
                return;
            }

            Color color = builder.Last.text.color;
            builder.Last.SetTextColor(new Color(color.r * 0.62f, color.g * 0.62f, color.b * 0.62f, color.a));
        }

        private static string ScanText(float seconds)
        {
            seconds = Mathf.Clamp(seconds, 0.2f, 2f);
            return Text.L("opt_scan", "Stock refresh interval (seconds)") + "：" + seconds.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static void GeneralChanged(System.Action apply)
        {
            if (filling)
            {
                return;
            }

            apply();
        }

        private static void FillShown(CustomUIBuilder builder)
        {
            filling = true;
            try
            {
                List<string> ids = ShownTypes.IdsInOrder();
                for (int i = 0; i < ids.Count; i++)
                {
                    string id = ids[i];
                    builder.AddToggle(ShownTypes.NameOf(id), ShownTypes.Shows(id), on => ShownChanged(id, on), "show_" + i);
                }
            }
            finally
            {
                filling = false;
            }
        }

        private static void ShownChanged(string id, bool on)
        {
            if (filling)
            {
                return;
            }

            ShownTypes.Set(id, on);
        }

        private static void FillTags(CustomUIBuilder builder)
        {
            filling = true;
            try
            {
                List<string> ids = DetailExclude.IdsInOrder();
                for (int i = 0; i < ids.Count; i++)
                {
                    string id = ids[i];
                    builder.AddToggle(TagCatalog.NameOf(id), DetailExclude.Contains(id), on => TagChanged(id, on), "tag_" + i);
                }
            }
            finally
            {
                filling = false;
            }
        }

        private static void TagChanged(string id, bool on)
        {
            if (filling)
            {
                return;
            }

            DetailExclude.Set(id, on);
        }

        private static void Fill()
        {
            if (listRoot == null || window == null || !window.IsAlive)
            {
                return;
            }

            CustomUIManager manager = CustomUIManager.Instance;
            if (manager == null)
            {
                return;
            }

            filling = true;
            try
            {
                CustomUIBuilder.ClearContainer(listRoot);
                var builder = new CustomUIBuilder(manager, window, listRoot);
                List<ItemExclude.Named> rows = ItemExclude.Filter(query, MatchLimit, out bool more);
                if (rows.Count == 0)
                {
                    builder.AddLabel(Text.L("exclude_empty", "No excluded items"), "exclude_empty");
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    ItemExclude.Named row = rows[i];
                    string id = row.Id;
                    builder.AddToggle(row.Name, ItemExclude.Contains(id), on => Changed(id, on), "pick_" + i);
                }

                if (more)
                {
                    builder.AddLabel(Text.L("exclude_more", "Keep typing to narrow the list"), "exclude_more");
                }

                CustomUIBuilder.RebuildLayout(listRoot);
            }
            finally
            {
                filling = false;
            }
        }

        private static void Changed(string id, bool on)
        {
            if (filling)
            {
                return;
            }

            ItemExclude.Set(id, on);
            if (string.IsNullOrEmpty(query))
            {
                refreshWhenIdle = true;
            }
        }
    }
}
