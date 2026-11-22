using UnityEngine;

namespace ShouldIRestock
{
    public static class StickyNote
    {
        private static Transform note;
        private static Transform face;
        private static SpriteRenderer renderer;
        private static SpriteRenderer halo;
        private static bool loggedMiss;
        private static readonly Color HoverColor = new Color32(255, 240, 160, 255);

        public static void Follow()
        {
            StorePhoneObject phone = FindPhone();
            if (phone == null)
            {
                if (note != null)
                {
                    note.gameObject.SetActive(false);
                }

                return;
            }

            Bounds bounds;
            SpriteRenderer sample;
            if (!PhoneAnchor.TryGetBounds(phone, out bounds, out sample))
            {
                if (!loggedMiss)
                {
                    loggedMiss = true;
                    Log.Warn("电话 " + phone.name + " 没有可见精灵或碰撞体，便签没贴上。" + PhoneAnchor.Describe(phone));
                }

                return;
            }

            Ensure(phone.gameObject.layer);
            float width = Mathf.Max(0.2f, bounds.size.x * 0.34f);
            float spriteWidth = renderer.sprite != null ? renderer.sprite.bounds.size.x : 1f;
            float scale = spriteWidth > 0.001f ? width / spriteWidth : 1f;
            note.position = new Vector3(bounds.center.x - width * 0.15f, bounds.min.y - width * 0.12f, bounds.center.z);
            note.localScale = new Vector3(scale, scale, 1f);
            if (sample != null)
            {
                renderer.sortingLayerID = sample.sortingLayerID;
                renderer.sortingOrder = sample.sortingOrder + 8;
                halo.sortingLayerID = sample.sortingLayerID;
                halo.sortingOrder = renderer.sortingOrder - 1;
            }

            note.gameObject.SetActive(true);
        }

        public static void PollClick()
        {
            if (note == null || !note.gameObject.activeInHierarchy)
            {
                return;
            }

            bool over = IsPointerOver();
            halo.enabled = over;
            if (over && Input.GetMouseButtonDown(0))
            {
                PaperBoard.Toggle(StockScanner.Scan());
            }
        }

        public static void Destroy()
        {
            PhoneAnchor.Forget();
            if (note != null)
            {
                Object.Destroy(note.gameObject);
            }

            note = null;
            face = null;
            renderer = null;
            halo = null;
        }

        private static bool IsPointerOver()
        {
            Camera cam = Camera.main;
            if (cam == null || renderer == null || renderer.sprite == null)
            {
                return false;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.forward, note.position);
            float distance;
            if (!plane.Raycast(ray, out distance))
            {
                return false;
            }

            Vector3 local = face.InverseTransformPoint(ray.GetPoint(distance));
            Bounds box = renderer.sprite.bounds;
            return box.Contains(new Vector3(local.x, local.y, box.center.z));
        }

        // 便签贴在桌面上：把平面内的两个轴（横向、纵深）按桌面透视拉斜，分解成 旋转 × 缩放 × 旋转 三层变换。
        private static void Ensure(int layer)
        {
            if (note != null)
            {
                return;
            }

            float outer, sx, sy, inner;
            DeskPlane.Decompose(out outer, out sx, out sy, out inner);
            var root = new GameObject("ShouldIRestock.Note");
            root.layer = layer;
            note = root.transform;
            note.rotation = Quaternion.Euler(0f, 0f, outer);
            var shear = new GameObject("Shear");
            shear.layer = layer;
            shear.transform.SetParent(note, false);
            shear.transform.localScale = new Vector3(sx, sy, 1f);
            var faceObject = new GameObject("Face");
            faceObject.layer = layer;
            face = faceObject.transform;
            face.SetParent(shear.transform, false);
            face.localRotation = Quaternion.Euler(0f, 0f, inner);
            renderer = faceObject.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite();
            BuildHalo(layer);
        }

        // 悬停描边：做一张外扩 1 像素的浅黄剪影，垫在便签后面。
        private static void BuildHalo(int layer)
        {
            var go = new GameObject("Halo");
            go.layer = layer;
            go.transform.SetParent(face, false);
            halo = go.AddComponent<SpriteRenderer>();
            halo.sprite = OutlineSprite(renderer.sprite);
            halo.color = HoverColor;
            halo.enabled = false;
        }

        private static Sprite OutlineSprite(Sprite source)
        {
            Texture2D tex = source.texture;
            int w = tex.width;
            int h = tex.height;
            Color32[] src = tex.GetPixels32();
            int ow = w + 2;
            int oh = h + 2;
            var pixels = new Color32[ow * oh];
            var fill = new Color32(255, 255, 255, 255);
            for (int y = 0; y < oh; y++)
            {
                for (int x = 0; x < ow; x++)
                {
                    if (Covered(src, w, h, x - 1, y - 1))
                    {
                        pixels[y * ow + x] = fill;
                    }
                }
            }

            var outline = new Texture2D(ow, oh, TextureFormat.RGBA32, false);
            outline.SetPixels32(pixels);
            outline.Apply();
            outline.filterMode = FilterMode.Point;
            return Sprite.Create(outline, new Rect(0f, 0f, ow, oh), new Vector2(0.5f, (h + 1f) / oh), source.pixelsPerUnit);
        }

        private static bool Covered(Color32[] src, int w, int h, int cx, int cy)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x >= 0 && y >= 0 && x < w && y < h && src[y * w + x].a > 16)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        private static Sprite LoadSprite()
        {
            try
            {
                byte[] bytes = ModHelper.ReadModFileBytes(ModIds.Id, "Textures/Paper/sticky_note.png");
                if (bytes != null && bytes.Length > 0)
                {
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (texture.LoadImage(bytes))
                    {
                        texture.filterMode = FilterMode.Point;
                        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 1f), 16f);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Warn("便签图读取失败：" + ex.Message);
            }

            var fallback = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color[256];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(0.96f, 0.84f, 0.36f, 1f);
            }

            fallback.SetPixels(pixels);
            fallback.Apply();
            fallback.filterMode = FilterMode.Point;
            return Sprite.Create(fallback, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 1f), 16f);
        }

        private static StorePhoneObject FindPhone()
        {
            try
            {
                return Object.FindObjectOfType<StorePhoneObject>();
            }
            catch
            {
                return null;
            }
        }
    }

    public static class PhoneAnchor
    {
        private static bool cached;
        private static Bounds cachedBounds;
        private static SpriteRenderer cachedSample;

        public static bool TryGetBounds(StorePhoneObject phone, out Bounds bounds, out SpriteRenderer sample)
        {
            if (cached && cachedSample != null)
            {
                bounds = cachedBounds;
                sample = cachedSample;
                return true;
            }

            bool ok = Compute(phone, out bounds, out sample);
            if (ok && sample != null)
            {
                cached = true;
                cachedBounds = bounds;
                cachedSample = sample;
            }

            return ok;
        }

        public static void Forget()
        {
            cached = false;
            cachedSample = null;
        }

        private static bool Compute(StorePhoneObject phone, out Bounds bounds, out SpriteRenderer sample)
        {
            bounds = new Bounds(phone.transform.position, Vector3.zero);
            sample = null;
            SpriteRenderer own = phone.GetComponentInChildren<SpriteRenderer>(true);
            Bounds opaque;
            if (own != null && SpriteOpaqueBounds.TryGet(own, out opaque))
            {
                bounds = opaque;
                sample = own;
                return true;
            }

            bool any = false;
            any = Encapsulate(phone.GetComponentsInChildren<Renderer>(true), ref bounds, ref any, ref sample);
            Collider2D[] colliders = phone.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Bounds box = colliders[i].bounds;
                if (box.size.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                if (!any)
                {
                    bounds = box;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(box);
                }
            }

            if (any && bounds.size.sqrMagnitude > 0.0001f)
            {
                return true;
            }

            SpriteRenderer[] sprites = Object.FindObjectsOfType<SpriteRenderer>();
            float best = float.MaxValue;
            for (int i = 0; i < sprites.Length; i++)
            {
                string name = sprites[i].gameObject.name;
                if (name.IndexOf("phone", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                float distance = (sprites[i].bounds.center - phone.transform.position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    sample = sprites[i];
                    bounds = sprites[i].bounds;
                    any = true;
                }
            }

            return any && bounds.size.sqrMagnitude > 0.0001f;
        }

        public static string Describe(StorePhoneObject phone)
        {
            var text = " 子物体:";
            int count = phone.transform.childCount;
            int limit = count < 8 ? count : 8;
            for (int i = 0; i < limit; i++)
            {
                text += " " + phone.transform.GetChild(i).name;
            }

            Renderer[] renderers = phone.GetComponentsInChildren<Renderer>(true);
            text += " 渲染器=" + renderers.Length;
            if (renderers.Length > 0)
            {
                text += " 范围=" + renderers[0].bounds;
            }

            return text;
        }

        private static bool Encapsulate(Renderer[] renderers, ref Bounds bounds, ref bool any, ref SpriteRenderer sample)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (sample == null)
                {
                    sample = renderers[i] as SpriteRenderer;
                }

                if (!any)
                {
                    bounds = renderers[i].bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return any;
        }
    }
}
