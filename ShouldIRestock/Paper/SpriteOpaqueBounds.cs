using UnityEngine;

namespace ShouldIRestock
{
    public static class SpriteOpaqueBounds
    {
        public static bool TryGet(SpriteRenderer renderer, out Bounds world)
        {
            world = default(Bounds);
            if (renderer == null || renderer.sprite == null || renderer.sprite.texture == null)
            {
                return false;
            }

            Sprite sprite = renderer.sprite;
            Rect rect = sprite.textureRect;
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);
            if (width <= 0 || height <= 0)
            {
                return false;
            }

            Texture2D copy = ReadBack(sprite.texture, rect, width, height);
            if (copy == null)
            {
                return false;
            }

            Color32[] pixels = copy.GetPixels32();
            Object.Destroy(copy);
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[row + x].a < 24)
                    {
                        continue;
                    }

                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return false;
            }
            Bounds full = renderer.bounds;
            float flipX = renderer.flipX ? -1f : 1f;
            float fx0 = minX / (float)width;
            float fx1 = (maxX + 1) / (float)width;
            float fy0 = minY / (float)height;
            float fy1 = (maxY + 1) / (float)height;
            if (flipX < 0f)
            {
                float swap = fx0;
                fx0 = 1f - fx1;
                fx1 = 1f - swap;
            }

            if (renderer.flipY)
            {
                float swap = fy0;
                fy0 = 1f - fy1;
                fy1 = 1f - swap;
            }

            Vector3 min = new Vector3(full.min.x + fx0 * full.size.x, full.min.y + fy0 * full.size.y, full.center.z);
            Vector3 max = new Vector3(full.min.x + fx1 * full.size.x, full.min.y + fy1 * full.size.y, full.center.z);
            world = new Bounds((min + max) * 0.5f, max - min);
            return true;
        }

        private static Texture2D ReadBack(Texture source, Rect rect, int width, int height)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture temp = null;
            try
            {
                temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Graphics.Blit(source, temp);
                RenderTexture.active = temp;
                var copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
                copy.Apply(false);
                return copy;
            }
            catch
            {
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (temp != null)
                {
                    RenderTexture.ReleaseTemporary(temp);
                }
            }
        }
    }
}
