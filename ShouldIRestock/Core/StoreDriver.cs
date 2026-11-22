using UnityEngine;

namespace ShouldIRestock
{
    public sealed class StoreDriver : MonoBehaviour
    {
        private float nextScan;
        private static StoreDriver current;

        public static void Ensure()
        {
            if (current != null)
            {
                return;
            }

            var host = new GameObject("ShouldIRestock.Driver");
            DontDestroyOnLoad(host);
            current = host.AddComponent<StoreDriver>();
        }

        private void Update()
        {
            ItemExcludeWindow.Tick();
            StickyNote.Follow();
            StickyNote.PollClick();
            if (Time.unscaledTime < nextScan)
            {
                return;
            }

            nextScan = Time.unscaledTime + Settings.Interval;
            StockSnapshot shot = StockScanner.Scan();
            PaperBoard.Sync(shot);
            OutlinePainter.Repaint(shot);
        }

        public static void Wake()
        {
            Ensure();
            if (current != null)
            {
                current.nextScan = 0f;
            }
        }
    }
}
