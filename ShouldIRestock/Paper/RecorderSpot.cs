using UnityEngine;

namespace ShouldIRestock
{
    public static class RecorderSpot
    {
        private static bool logged;

        // 右边只改左右位置。上下高度和贴在电话下沿时相同，并比平板电脑右缘再往左靠。
        public static bool TryGet(float noteWidth, Vector3 leftSpot, out Vector3 world)
        {
            world = leftSpot;
            StoreTablet tablet = Nearest(leftSpot);
            Bounds box;
            if (tablet == null || !TryBounds(tablet, out box))
            {
                WarnOnce(leftSpot);
                return false;
            }

            world = new Vector3(box.max.x - noteWidth * 0.15f, leftSpot.y, leftSpot.z);
            if (!logged)
            {
                logged = true;
                Log.Msg("便签贴在平板电脑右侧，高度与左侧一致：" + tablet.name + " 范围=" + box + " 落点=" + world);
            }

            return true;
        }

        private static StoreTablet Nearest(Vector3 phoneCenter)
        {
            StoreTablet[] tablets;
            try
            {
                tablets = Object.FindObjectsOfType<StoreTablet>();
            }
            catch
            {
                return null;
            }

            StoreTablet best = null;
            float bestDistance = 20f * 20f;
            for (int i = 0; i < tablets.Length; i++)
            {
                if (tablets[i] == null)
                {
                    continue;
                }

                float distance = (tablets[i].transform.position - phoneCenter).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = tablets[i];
                }
            }

            return best;
        }

        private static bool TryBounds(StoreTablet tablet, out Bounds bounds)
        {
            bounds = new Bounds(tablet.transform.position, Vector3.zero);
            Collider2D collider = tablet.GetComponent<Collider2D>();
            if (collider == null)
            {
                collider = tablet.GetComponentInChildren<Collider2D>(true);
            }

            if (collider != null && collider.bounds.size.sqrMagnitude > 0.01f)
            {
                bounds = collider.bounds;
                return true;
            }

            Renderer renderer = tablet.GetComponentInChildren<Renderer>(true);
            if (renderer != null && renderer.bounds.size.sqrMagnitude > 0.01f)
            {
                bounds = renderer.bounds;
                return true;
            }

            bounds = new Bounds(tablet.transform.position, new Vector3(0.9f, 1f, 0.1f));
            return true;
        }

        private static void WarnOnce(Vector3 phoneCenter)
        {
            if (logged)
            {
                return;
            }

            logged = true;
            Log.Warn("没找到电话附近的平板电脑，便签仍贴在电话下沿。电话位置=" + phoneCenter);
        }
    }
}
