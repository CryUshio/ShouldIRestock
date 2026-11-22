using UnityEngine;
using UnityEngine.UI;

namespace ShouldIRestock
{
    public static class StickyNote
    {
        private static RectTransform note;
        private static bool loggedMiss;

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

            RectTransform phoneRect = phone.GetComponent<RectTransform>();
            if (phoneRect == null)
            {
                PlaceWorld(phone);
                return;
            }

            PlaceUi(phoneRect);
        }

        public static void Destroy()
        {
            if (note != null)
            {
                Object.Destroy(note.gameObject);
                note = null;
            }
        }

        private static void PlaceUi(RectTransform phoneRect)
        {
            Ensure(phoneRect);
            note.anchorMin = new Vector2(0.5f, 0f);
            note.anchorMax = new Vector2(0.5f, 0f);
            note.pivot = new Vector2(0.5f, 1f);
            note.sizeDelta = new Vector2(36f, 36f);
            note.anchoredPosition = new Vector2(0f, -4f);
            note.SetAsLastSibling();
            note.gameObject.SetActive(true);
        }

        private static void PlaceWorld(StorePhoneObject phone)
        {
            Canvas canvas = null;
            var canvases = Object.FindObjectsOfType<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].isRootCanvas && canvases[i].renderMode != RenderMode.WorldSpace)
                {
                    canvas = canvases[i];
                    break;
                }
            }

            if (canvas == null)
            {
                if (!loggedMiss)
                {
                    loggedMiss = true;
                    Log.Warn("找到了电话，但它不在界面上，也没有可用的屏幕画布，便签没贴上。");
                }

                return;
            }

            Ensure(canvas.transform as RectTransform);
            SpriteRenderer sprite = phone.GetComponent<SpriteRenderer>();
            Vector3 world = sprite != null
                ? new Vector3(sprite.bounds.center.x, sprite.bounds.min.y, sprite.bounds.center.z)
                : phone.transform.position;
            Camera camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector2 screen = camera.WorldToScreenPoint(world);
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(note.parent as RectTransform, screen, camera, out local))
            {
                return;
            }

            note.gameObject.SetActive(true);
            note.pivot = new Vector2(0.5f, 1f);
            note.sizeDelta = new Vector2(36f, 36f);
            note.anchoredPosition = local + new Vector2(0f, -4f);
        }

        private static void Ensure(RectTransform parent)
        {
            if (note != null && note.parent != parent)
            {
                Destroy();
            }

            if (note != null)
            {
                return;
            }

            var go = new GameObject("ShouldIRestock.Note", typeof(RectTransform));
            note = go.GetComponent<RectTransform>();
            note.SetParent(parent, false);
            note.sizeDelta = new Vector2(36f, 36f);
            var image = go.AddComponent<Image>();
            image.sprite = LoadSprite();
            image.color = Color.white;
            image.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnClick);
        }

        private static void OnClick()
        {
            PaperBoard.Toggle(StockScanner.Scan());
        }

        private static Sprite LoadSprite()
        {
            try
            {
                byte[] bytes = ModHelper.ReadModFileBytes(ModIds.Id, "Textures/Paper/sticky_note.png");
                if (bytes == null || bytes.Length == 0)
                {
                    return null;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    return null;
                }

                texture.filterMode = FilterMode.Point;
                return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
            }
            catch (System.Exception ex)
            {
                Log.Warn("便签图读取失败：" + ex.Message);
                return null;
            }
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
}
