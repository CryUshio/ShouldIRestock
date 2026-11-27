namespace ShouldIRestock
{
    public static class TagHighlighter
    {
        public static string Hover { get; private set; }
        public static string Locked { get; private set; }

        public static string Active => Locked ?? Hover;

        public static void SetHover(string tagId)
        {
            Hover = tagId;
            StoreDriver.Wake();
            PaperBoard.RefreshMarks();
        }

        public static void ClearHover(string tagId)
        {
            if (Hover == tagId)
            {
                Hover = null;
                StoreDriver.Wake();
                PaperBoard.RefreshMarks();
            }
        }

        public static void ClearHover()
        {
            if (Hover == null)
            {
                return;
            }

            Hover = null;
            StoreDriver.Wake();
            PaperBoard.RefreshMarks();
        }

        // 便签关掉后，悬停和锁定一起取消，框线马上拆掉。
        public static void Release()
        {
            Hover = null;
            Locked = null;
            OutlinePainter.Clear();
            TabPlatePainter.Clear();
        }

        public static void ToggleLock(string tagId)
        {
            Locked = Locked == tagId ? null : tagId;
            if (Locked != null)
            {
                Hover = null;
            }

            StoreDriver.Wake();
            PaperBoard.RefreshMarks();
        }
    }
}
