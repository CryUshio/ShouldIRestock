using System.Text;
using UnityEngine;

namespace ShouldIRestock
{
    public static class SceneProbe
    {
        private static bool dumped;

        public static void DumpOnce()
        {
            if (dumped)
            {
                return;
            }

            dumped = true;
            DumpPhone();
            DumpSample();
        }

        private static void DumpPhone()
        {
            StorePhoneObject phone = null;
            try
            {
                phone = Object.FindObjectOfType<StorePhoneObject>();
            }
            catch
            {
                phone = null;
            }

            if (phone == null)
            {
                Log.Msg("探针：场景里没有 StorePhoneObject。");
                return;
            }

            var chain = new StringBuilder(phone.name);
            Transform node = phone.transform.parent;
            Canvas canvas = phone.GetComponentInParent<Canvas>();
            int guard = 0;
            while (node != null && guard++ < 8)
            {
                chain.Append(" < ").Append(node.name);
                node = node.parent;
            }

            RectTransform rect = phone.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Log.Msg("探针：电话 " + chain + " 画布=" + (canvas != null ? canvas.name : "无")
                    + " 锚点=" + rect.anchoredPosition + " 尺寸=" + rect.rect.size
                    + " 世界左下=" + corners[0] + " 世界右下=" + corners[3]);
                return;
            }

            Log.Msg("探针：电话 " + chain + " 没有 RectTransform，世界坐标=" + phone.transform.position
                + " 画布=" + (canvas != null ? canvas.name : "无"));
        }

        private static void DumpSample()
        {
            StockSnapshot shot = StockScanner.Scan();
            int limit = Mathf.Min(12, shot.Pieces.Count);
            Log.Msg("探针：库存 " + shot.Pieces.Count + " 件，有件数的品类 " + shot.Rows.Count
                + " 个，赃物 " + shot.Stolen + "，违禁品 " + shot.Contraband + "。");
            for (int i = 0; i < limit; i++)
            {
                StockPiece piece = shot.Pieces[i];
                string id = "?";
                try
                {
                    id = piece.Item.identifier;
                }
                catch
                {
                }

                Log.Msg("探针物品 " + id + " 标签=" + string.Join(",", piece.Tags)
                    + " 赃=" + piece.Stolen + " 违=" + piece.Contraband);
            }
        }
    }
}
