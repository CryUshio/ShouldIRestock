using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShouldIRestock
{
    public sealed class ItemFrame
    {
        public GameObject Root;
        public RectTransform Rect;
        public ItemFrameGraphic Outline;
        public bool Visible;

        public static ItemFrame Create(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var root = new GameObject(ModIds.FrameNode);
            root.transform.SetParent(parent, false);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            var outline = root.AddComponent<ItemFrameGraphic>();
            outline.raycastTarget = false;
            return new ItemFrame { Root = root, Rect = rect, Outline = outline };
        }

        public void Apply(Color color, bool[] cells, int cols, int rows, float width, float height, bool flipped)
        {
            if (Outline == null)
            {
                return;
            }

            Root.SetActive(true);
            Visible = true;
            Outline.color = color;
            Outline.Configure(cells, cols, rows, width, height, Settings.LineThickness);
            float inheritedMirror = Rect.parent != null && Rect.parent.localScale.x < 0f ? -1f : 1f;
            float mirror = (flipped ? -1f : 1f) * inheritedMirror;
            Root.transform.localScale = new Vector3(mirror, 1f, 1f);
        }

        public void ApplyRect(Color color, float width, float height)
        {
            if (Outline == null || width < 1f || height < 1f)
            {
                return;
            }

            Root.SetActive(true);
            Visible = true;
            Outline.color = color;
            Outline.Configure(null, 1, 1, width, height, Settings.LineThickness);
            Root.transform.localScale = Vector3.one;
            Root.transform.SetAsLastSibling();
        }

        public void Hide()
        {
            if (!Visible || Root == null)
            {
                Visible = false;
                return;
            }

            Visible = false;
            Root.SetActive(false);
        }

        public void Destroy()
        {
            if (Root != null)
            {
                Object.Destroy(Root);
            }

            Root = null;
        }
    }

    public static class OutlinePainter
    {
        private static readonly Dictionary<GameItemElement, ItemFrame> Frames = new Dictionary<GameItemElement, ItemFrame>();

        public static void Repaint(StockSnapshot shot)
        {
            string active = TagHighlighter.Active;
            var alive = new HashSet<GameItemElement>();
            if (shot != null && active != null)
            {
                for (int i = 0; i < shot.Pieces.Count; i++)
                {
                    StockPiece piece = shot.Pieces[i];
                    if (!Matches(piece, active) || piece.Element == null || !Shown(piece.Element))
                    {
                        continue;
                    }

                    ItemFrame frame = Ensure(piece.Element);
                    if (frame == null)
                    {
                        continue;
                    }

                    alive.Add(piece.Element);
                    Paint(frame, piece, active);
                }
            }

            var stale = new List<GameItemElement>();
            foreach (KeyValuePair<GameItemElement, ItemFrame> pair in Frames)
            {
                if (!alive.Contains(pair.Key) || pair.Key == null)
                {
                    pair.Value.Hide();
                    if (pair.Key == null || Gone(pair.Key))
                    {
                        pair.Value.Destroy();
                        stale.Add(pair.Key);
                    }
                }
            }

            for (int i = 0; i < stale.Count; i++)
            {
                Frames.Remove(stale[i]);
            }
        }

        public static void Clear()
        {
            foreach (KeyValuePair<GameItemElement, ItemFrame> pair in Frames)
            {
                pair.Value.Destroy();
            }

            Frames.Clear();
        }

        private static bool Matches(StockPiece piece, string active)
        {
            if (piece == null || piece.Item == null)
            {
                return false;
            }

            return piece.Has(active);
        }

        private static void Paint(ItemFrame frame, StockPiece piece, string active)
        {
            RectTransform anchor;
            float width;
            float height;
            bool flipped;
            GridShape shape;
            if (!Measure(piece.Element, out anchor, out width, out height, out flipped, out shape))
            {
                frame.Hide();
                return;
            }

            if (frame.Rect != null && frame.Rect.parent != anchor)
            {
                frame.Destroy();
                Frames.Remove(piece.Element);
                frame = ItemFrame.Create(anchor);
                if (frame == null)
                {
                    return;
                }

                Frames[piece.Element] = frame;
            }

            int cols;
            int rows;
            bool[] cells = ShapeMask.Read(shape, out cols, out rows);
            frame.Apply(TagCatalog.OutlineColor(piece.Style(active)), cells, cols, rows, width, height, flipped);
        }

        private static ItemFrame Ensure(GameItemElement element)
        {
            ItemFrame frame;
            Frames.TryGetValue(element, out frame);
            if (frame != null && frame.Root == null)
            {
                Frames.Remove(element);
                frame = null;
            }

            if (frame != null)
            {
                return frame;
            }

            RectTransform anchor;
            float width;
            float height;
            bool flipped;
            GridShape shape;
            if (!Measure(element, out anchor, out width, out height, out flipped, out shape))
            {
                return null;
            }

            frame = ItemFrame.Create(anchor);
            if (frame != null)
            {
                Frames[element] = frame;
            }

            return frame;
        }

        private static bool Measure(GameItemElement element, out RectTransform anchor, out float width, out float height, out bool flipped, out GridShape shape)
        {
            anchor = null;
            width = 0f;
            height = 0f;
            flipped = false;
            shape = null;
            try
            {
                GridShape original = element.shape;
                GridShape modified = element.modifiedShape;
                flipped = (original != null && original.flipped) || (modified != null && modified.flipped);
                bool rotated = (original != null && original.orientation != 0)
                    || (modified != null && modified.orientation != 0);

                // 与 hollow.clientdemandhighlight 一致：普通物品的形状由 rotator 负责旋转，
                // 这里仍读原始 mask，避免把 modifiedShape 的旋转再应用一次。
                // 只有模块化枪械需要用 modifiedShape（它包含配件后的真实轮廓）。
                bool modularGun = element.IsTag("ADV_GUN_TAG");
                shape = modularGun ? (modified ?? original) : original;
                if (rotated && element.rotator != null)
                {
                    anchor = element.rotator.rectTransform;
                }

                if (anchor == null)
                {
                    anchor = element.rectTransform;
                }

                if (anchor == null)
                {
                    return false;
                }

                Rect rect = anchor.rect;
                width = rect.width;
                height = rect.height;
                return width >= 1f && height >= 1f;
            }
            catch
            {
                return false;
            }
        }

        private static bool Shown(GameItemElement element)
        {
            try
            {
                return element != null && element.rectTransform != null && element.rectTransform.gameObject.activeInHierarchy;
            }
            catch
            {
                return false;
            }
        }

        private static bool Gone(GameItemElement element)
        {
            try
            {
                return element == null || element.IsDestroyed();
            }
            catch
            {
                return true;
            }
        }
    }
}
