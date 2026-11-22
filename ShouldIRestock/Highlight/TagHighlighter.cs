namespace ShouldIRestock
{
    public static class TagHighlighter
    {
        public static string Hover { get; private set; }
        public static string Locked { get; private set; }

        public static string Active => Locked ?? Hover;

        public static void SetHover(string tagId)
        {
            if (Locked != null)
            {
                return;
            }

            Hover = tagId;
            StoreDriver.Wake();
        }

        public static void ClearHover(string tagId)
        {
            if (Hover == tagId)
            {
                Hover = null;
                StoreDriver.Wake();
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
        }

        public static void ToggleLock(string tagId)
        {
            Locked = Locked == tagId ? null : tagId;
            if (Locked != null)
            {
                Hover = null;
            }

            StoreDriver.Wake();
        }
    }
}
