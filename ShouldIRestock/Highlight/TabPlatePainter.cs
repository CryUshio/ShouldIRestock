using System.Collections.Generic;
using UnityEngine;

namespace ShouldIRestock
{
    // 带分页标签的箱子：打开后按 hollow.clientdemandhighlight 的做法，把装着命中货的标签页也描一圈。
    public static class TabPlatePainter
    {
        private sealed class Page
        {
            public RectTransform Plate;
            public ItemFrame Frame;
        }

        private sealed class Book
        {
            public int Count;
            public readonly List<Page> Pages = new List<Page>();
        }

        private static readonly Dictionary<MachineryTabs, Book> Books = new Dictionary<MachineryTabs, Book>();

        public static void Repaint(StockSnapshot shot)
        {
            string active = TagHighlighter.Active;
            if (shot == null || active == null)
            {
                HideAll();
                return;
            }

            var alive = new HashSet<MachineryTabs>();
            for (int i = 0; i < shot.Pieces.Count; i++)
            {
                PaintBox(shot.Pieces[i].Item, shot, active, alive);
            }

            var stale = new List<MachineryTabs>();
            foreach (KeyValuePair<MachineryTabs, Book> pair in Books)
            {
                if (pair.Key == null)
                {
                    DestroyBook(pair.Value);
                    stale.Add(pair.Key);
                    continue;
                }

                if (!alive.Contains(pair.Key))
                {
                    HideBook(pair.Value);
                }
            }

            for (int i = 0; i < stale.Count; i++)
            {
                Books.Remove(stale[i]);
            }
        }

        public static void Clear()
        {
            foreach (KeyValuePair<MachineryTabs, Book> pair in Books)
            {
                DestroyBook(pair.Value);
            }

            Books.Clear();
        }

        private static void HideAll()
        {
            foreach (KeyValuePair<MachineryTabs, Book> pair in Books)
            {
                HideBook(pair.Value);
            }
        }

        private static void PaintBox(GameItem box, StockSnapshot shot, string active, HashSet<MachineryTabs> alive)
        {
            if (!Opened(box))
            {
                return;
            }

            MachineryTabs tabs = TabsOf(box);
            if (tabs == null)
            {
                return;
            }

            int count;
            try
            {
                count = tabs.count;
            }
            catch
            {
                return;
            }

            if (count <= 0)
            {
                return;
            }

            Book book;
            if (!Books.TryGetValue(tabs, out book))
            {
                book = new Book();
                Books[tabs] = book;
            }

            alive.Add(tabs);
            if (book.Count != count)
            {
                Rebuild(book, tabs, count);
            }

            for (int i = 0; i < book.Pages.Count; i++)
            {
                HighlightStyle style = StyleOf(PageInventory(tabs, i), shot, active);
                Page page = book.Pages[i];
                if (style == HighlightStyle.None || page.Plate == null)
                {
                    if (page.Frame != null)
                    {
                        page.Frame.Hide();
                    }

                    continue;
                }

                Show(page, TagCatalog.OutlineColor(style));
            }
        }

        private static HighlightStyle StyleOf(GameInventory page, StockSnapshot shot, string active)
        {
            HighlightStyle best = HighlightStyle.None;
            if (page == null || shot == null)
            {
                return best;
            }

            List<GameItem> items = null;
            try
            {
                items = page.childItems;
            }
            catch
            {
                items = null;
            }

            if (items == null)
            {
                return best;
            }

            for (int i = 0; i < items.Count && best != HighlightStyle.Contraband; i++)
            {
                StockPiece piece;
                if (items[i] == null || !shot.Index.TryGetValue(items[i], out piece))
                {
                    continue;
                }

                HighlightStyle style = piece.Style(active);
                if (style > best)
                {
                    best = style;
                }
            }

            return best;
        }

        private static void Show(Page page, Color color)
        {
            if (page.Frame == null || page.Frame.Root == null || page.Frame.Rect.parent != page.Plate)
            {
                if (page.Frame != null)
                {
                    page.Frame.Destroy();
                }

                page.Frame = ItemFrame.Create(page.Plate);
            }

            if (page.Frame == null || page.Frame.Rect == null)
            {
                return;
            }

            Rect rect = page.Frame.Rect.rect;
            page.Frame.ApplyRect(color, rect.width, rect.height);
        }

        private static void Rebuild(Book book, MachineryTabs tabs, int count)
        {
            for (int i = 0; i < book.Pages.Count; i++)
            {
                if (book.Pages[i].Frame != null)
                {
                    book.Pages[i].Frame.Destroy();
                }
            }

            book.Pages.Clear();
            List<RectTransform> plates = Plates(tabs, count);
            for (int i = 0; i < count; i++)
            {
                book.Pages.Add(new Page { Plate = i < plates.Count ? plates[i] : null });
            }

            book.Count = count;
        }

        // 标签按钮是页数两倍个子节点里的偶数位，和 hollow 的 TabPainter 一样。
        private static List<RectTransform> Plates(MachineryTabs tabs, int count)
        {
            var plates = new List<RectTransform>();
            try
            {
                RectTransform root = tabs.rectTransform;
                if (root == null)
                {
                    return plates;
                }

                Transform row = null;
                for (int i = 0; i < root.childCount; i++)
                {
                    Transform child = root.GetChild(i);
                    if (child != null && child.childCount == count * 2)
                    {
                        row = child;
                        break;
                    }
                }

                if (row == null)
                {
                    return plates;
                }

                for (int i = 0; i < count; i++)
                {
                    Transform plate = row.GetChild(i * 2);
                    RectTransform rect = plate as RectTransform;
                    if (rect == null)
                    {
                        plates.Clear();
                        return plates;
                    }

                    plates.Add(rect);
                }
            }
            catch
            {
                plates.Clear();
            }

            return plates;
        }

        private static GameInventory PageInventory(MachineryTabs tabs, int index)
        {
            try
            {
                PixelElement page = tabs.GetPage(index);
                if (page == null)
                {
                    return null;
                }

                MachineryIntake intake = page as MachineryIntake;
                if (intake != null)
                {
                    return intake.content as GameInventory;
                }

                return page as GameInventory;
            }
            catch
            {
                return null;
            }
        }

        private static MachineryTabs TabsOf(GameItem box)
        {
            try
            {
                PixelWindow window = box.contentWindow;
                PixelElement child = window != null ? window.childElement : null;
                MachineryPanelRow row = child as MachineryPanelRow;
                if (row == null)
                {
                    return null;
                }

                MachineryPanel storage = row.GetPanel("storage");
                PixelElement content = storage != null ? storage.content : null;
                MachineryTabs tabs = content as MachineryTabs;
                if (tabs != null)
                {
                    return tabs;
                }

                IReadOnlyList<MachineryPanel> panels = row.Panels;
                if (panels == null)
                {
                    return null;
                }

                for (int i = 0; i < panels.Count; i++)
                {
                    MachineryPanel panel = panels[i];
                    if (panel == null)
                    {
                        continue;
                    }

                    tabs = panel.content as MachineryTabs;
                    if (tabs != null)
                    {
                        return tabs;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static bool Opened(GameItem item)
        {
            try
            {
                PixelWindow window = item != null ? item.contentWindow : null;
                return window != null && window.IsVisible();
            }
            catch
            {
                return false;
            }
        }

        private static void HideBook(Book book)
        {
            if (book == null)
            {
                return;
            }

            for (int i = 0; i < book.Pages.Count; i++)
            {
                if (book.Pages[i].Frame != null)
                {
                    book.Pages[i].Frame.Hide();
                }
            }
        }

        private static void DestroyBook(Book book)
        {
            if (book == null)
            {
                return;
            }

            for (int i = 0; i < book.Pages.Count; i++)
            {
                if (book.Pages[i].Frame != null)
                {
                    book.Pages[i].Frame.Destroy();
                }
            }

            book.Pages.Clear();
            book.Count = 0;
        }
    }
}
