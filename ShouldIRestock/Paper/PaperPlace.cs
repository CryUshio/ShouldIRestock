using System.Globalization;
using UnityEngine;

namespace ShouldIRestock
{
    // 记下便签被拖到的位置。分辨率变了以后，至少留一块在画面里，避免整张纸落到屏幕外。
    public static class PaperPlace
    {
        private const string FileName = "paper-place.txt";
        private const int DockWait = 8;

        private static bool loaded;
        private static bool has;
        private static float x;
        private static float y;
        private static bool restored;
        private static bool settled;
        private static int waits;
        private static float seenWidth;
        private static float seenHeight;

        public static void Arm()
        {
            restored = false;
            settled = false;
            waits = 0;
            seenWidth = 0f;
            seenHeight = 0f;
        }

        public static void Capture(PaperUI paper)
        {
            if (!settled || paper == null || !paper.IsAlive || paper.Rect == null)
            {
                return;
            }

            Store(paper.Rect.anchoredPosition);
        }

        public static void Tick(PaperUI paper)
        {
            if (paper == null || !paper.IsAlive || paper.Rect == null)
            {
                return;
            }

            RectTransform rect = paper.Rect;
            if (!restored)
            {
                if (!Ready(rect))
                {
                    return;
                }

                Restore(rect);
                restored = true;
                settled = true;
                NoteParent(rect);
                return;
            }

            if (ParentChanged(rect))
            {
                PullInside(rect);
            }

            Store(rect.anchoredPosition);
        }

        private static bool Ready(RectTransform rect)
        {
            RectTransform parent = rect.parent as RectTransform;
            if (parent == null || parent.rect.width < 1f || parent.rect.height < 1f)
            {
                return false;
            }

            RectTransform dock = LetterboxManager.DockRoot;
            if (dock != null && parent != dock)
            {
                waits++;
                return waits > DockWait;
            }

            return true;
        }

        private static void Restore(RectTransform rect)
        {
            Load();
            if (has)
            {
                rect.anchoredPosition = new Vector2(x, y);
            }

            PullInside(rect);
            Store(rect.anchoredPosition);
        }

        private static bool ParentChanged(RectTransform rect)
        {
            RectTransform parent = rect.parent as RectTransform;
            if (parent == null)
            {
                return false;
            }

            Vector3[] corners = new Vector3[4];
            parent.GetWorldCorners(corners);
            float width = corners[2].x - corners[0].x;
            float height = corners[1].y - corners[0].y;
            if (width < 1f || height < 1f)
            {
                return false;
            }

            bool changed = Mathf.Abs(width - seenWidth) > 0.5f || Mathf.Abs(height - seenHeight) > 0.5f;
            seenWidth = width;
            seenHeight = height;
            return changed;
        }

        private static void NoteParent(RectTransform rect)
        {
            ParentChanged(rect);
        }

        private static void PullInside(RectTransform rect)
        {
            RectTransform parent = rect.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            Vector3[] area = new Vector3[4];
            Vector3[] box = new Vector3[4];
            parent.GetWorldCorners(area);
            rect.GetWorldCorners(box);
            float width = box[2].x - box[0].x;
            float height = box[1].y - box[0].y;
            float parentWidth = area[2].x - area[0].x;
            float parentHeight = area[1].y - area[0].y;
            if (width < 1f || height < 1f || parentWidth < 1f || parentHeight < 1f)
            {
                return;
            }

            float keepX = Mathf.Min(Mathf.Max(32f, width * 0.25f), parentWidth * 0.5f, width);
            float keepY = Mathf.Min(Mathf.Max(32f, height * 0.25f), parentHeight * 0.5f, height);
            Vector3 shift = Vector3.zero;
            if (box[2].x < area[0].x + keepX)
            {
                shift.x = area[0].x + keepX - box[2].x;
            }

            if (box[0].x > area[2].x - keepX)
            {
                shift.x = area[2].x - keepX - box[0].x;
            }

            if (box[1].y < area[0].y + keepY)
            {
                shift.y = area[0].y + keepY - box[1].y;
            }

            if (box[0].y > area[1].y - keepY)
            {
                shift.y = area[1].y - keepY - box[0].y;
            }

            if (shift.sqrMagnitude > 0.01f)
            {
                rect.position += shift;
            }
        }

        private static void Store(Vector2 position)
        {
            if (has && Mathf.Abs(position.x - x) < 0.5f && Mathf.Abs(position.y - y) < 0.5f)
            {
                return;
            }

            x = position.x;
            y = position.y;
            has = true;
            Save();
        }

        private static void Load()
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

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            float nextX = 0f;
            float nextY = 0f;
            bool gotX = false;
            bool gotY = false;
            string[] lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                int cut = line.IndexOf('=');
                if (cut <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, cut).Trim();
                string value = line.Substring(cut + 1).Trim();
                float number;
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
                {
                    continue;
                }

                if (key == "x")
                {
                    nextX = number;
                    gotX = true;
                }
                else if (key == "y")
                {
                    nextY = number;
                    gotY = true;
                }
            }

            if (!gotX || !gotY || float.IsNaN(nextX) || float.IsNaN(nextY) || float.IsInfinity(nextX) || float.IsInfinity(nextY))
            {
                return;
            }

            x = nextX;
            y = nextY;
            has = true;
        }

        private static void Save()
        {
            string text = "x=" + x.ToString("0.###", CultureInfo.InvariantCulture) + "\ny=" + y.ToString("0.###", CultureInfo.InvariantCulture);
            try
            {
                ModHelper.WriteModDataFileText(ModIds.Id, FileName, text);
            }
            catch
            {
            }
        }
    }
}
