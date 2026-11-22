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
        public Image StolenMark;
        public Image ContrabandMark;
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
            return new ItemFrame
            {
                Root = root,
                Rect = rect,
                Outline = outline,
                StolenMark = Mark(rect, "SIR_Stolen", new Vector2(0f, 1f), new Color(1f, 0.7f, 0.1f, 0.95f)),
                ContrabandMark = Mark(rect, "SIR_Contraband", new Vector2(1f, 1f), new Color(0.95f, 0.22f, 0.2f, 0.95f))
            };
        }

        public void Apply(Color color, bool[] cells, int cols, int rows, float width, float height, bool flipped, bool stolen, bool contraband)
        {
            if (Outline == null)
            {
                return;
            }

            Root.SetActive(true);
            Visible = true;
            Outline.color = color;
            Outline.Configure(cells, cols, rows, width, height, Settings.LineThickness);
            float mirror = flipped ? -1f : 1f;
            Root.transform.localScale = new Vector3(mirror, 1f, 1f);
            StolenMark.gameObject.SetActive(stolen);
            ContrabandMark.gameObject.SetActive(contraband);
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

        private static Image Mark(RectTransform parent, string name, Vector2 anchor, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = new Vector2(7f, 7f);
            rect.anchoredPosition = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            go.SetActive(false);
            return image;
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

            if (active == ModIds.Stolen)
            {
                return piece.Stolen;
            }

            if (active == ModIds.Contraband)
            {
                return piece.Contraband;
            }

            if (active == ModIds.Other)
            {
                return piece.Tags.Count == 0;
            }

            return piece.Tags.Contains(active);
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
            Color color = TagCatalog.ColorOf(active);
            bool markStolen = piece.Stolen && active != ModIds.Stolen;
            bool markContraband = piece.Contraband && active != ModIds.Contraband;
            frame.Apply(color, cells, cols, rows, width, height, flipped, markStolen, markContraband);
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
                shape = element.modifiedShape ?? element.shape;
                flipped = (element.shape != null && element.shape.flipped) || (element.modifiedShape != null && element.modifiedShape.flipped);
                bool rotated = element.shape != null && element.shape.orientation != 0;
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
