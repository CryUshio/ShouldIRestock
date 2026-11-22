using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShouldIRestock
{
    // 纸张界面自己的按钮。正式版没带贴图目录，所以先在已加载贴图里按原名找，找不到再用随模组带的原图。
    public static class BookChrome
    {
        private static readonly string[] SpriteKeys =
        {
            "paper_buttons_close1", "paper_buttons_screen_0", "paper_buttons_l_1", "paper_buttons1"
        };

        public static void Apply(PaperUI paper)
        {
            if (paper == null)
            {
                return;
            }

            UseGameSprites();

            // 顶部关闭和方框，底部左翻、页码、右翻。
            const float designWidth = 248f;
            const float designHeight = 340f;
            const float iconWidth = 14f;
            const float iconHeight = 15f;
            for (int i = 0; i < paper.Data.chrome.Count; i++)
            {
                PaperElementData element = paper.Data.chrome[i];
                switch (element.action)
                {
                    case "zoom":
                        element.x = designWidth - 41f;
                        element.y = 8f;
                        element.w = iconWidth;
                        element.h = iconHeight;
                        break;
                    case "close":
                        element.x = designWidth - 21f;
                        element.y = 8f;
                        element.w = iconWidth;
                        element.h = iconHeight;
                        break;
                    case "flip_left":
                        element.x = designWidth * 0.5f - 41f;
                        element.y = designHeight - 28f;
                        element.w = iconWidth;
                        element.h = iconHeight;
                        break;
                    case "page_indicator":
                        element.x = designWidth * 0.5f - 25f;
                        element.y = designHeight - 33f;
                        element.w = 50f;
                        element.h = 24f;
                        break;
                    case "flip_right":
                        element.x = designWidth * 0.5f + 27f;
                        element.y = designHeight - 28f;
                        element.w = iconWidth;
                        element.h = iconHeight;
                        break;
                }
            }
        }

        public static void Style(PaperUI paper, PaperUI.Binding binding)
        {
            if (binding == null || binding.data == null)
            {
                return;
            }

            string action = binding.data.action;
            if (action == "page_indicator" && binding.text != null)
            {
                PaperTextStyle style = PaperTextStyle.Get("page_number");
                TMP_FontAsset font = style.Font();
                if (font != null)
                {
                    binding.text.font = font;
                }

                binding.text.fontSize = style.size;
                binding.text.color = style.color;
                binding.text.alignment = TextAlignmentOptions.Center;
                binding.text.text = (paper.CurrentPage + 1) + "/" + paper.PageCount;
            }

            if (binding.image != null && binding.image.sprite != null && binding.image.sprite.texture != null
                && (action == "close" || action == "zoom" || action == "flip_left" || action == "flip_right"))
            {
                binding.image.sprite.texture.filterMode = FilterMode.Point;
                binding.image.color = Color.white;
            }

            if (action == "exclude" && binding.image != null)
            {
                binding.image.color = Color.white;
                binding.image.preserveAspect = true;
            }

            if (binding.button != null && (action == "close" || action == "zoom" || action == "exclude"
                || action == "flip_left" || action == "flip_right"))
            {
                ColorBlock colors = binding.button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 1f, 0.82f, 1f);
                colors.pressedColor = new Color(0.8f, 0.8f, 0.7f, 1f);
                colors.selectedColor = Color.white;
                binding.button.colors = colors;
            }
        }

        // 游戏按界面名字把缩放档位记进存档。便签每次重建都要带上同一个名字，关掉再开才回到上次的大小。
        public static void RememberZoom(PaperUI paper)
        {
            if (paper == null || paper.Root == null)
            {
                return;
            }

            StoreUI zoom = paper.Root.GetComponent<StoreUI>();
            if (zoom == null)
            {
                return;
            }

            zoom.identifier = ModIds.Id + "_" + ModIds.BookId;
            ApplySavedZoom(zoom);
        }

        private static void ApplySavedZoom(StoreUI zoom)
        {
            int level = 1;
            if (PlayerStore.Instance != null && PlayerStore.Instance.storeUIZoomLevels != null)
            {
                int saved;
                if (PlayerStore.Instance.storeUIZoomLevels.TryGetValue(zoom.identifier, out saved))
                {
                    level = saved;
                }
            }

            Transform target = zoom.zoomRoot != null ? zoom.zoomRoot : zoom.transform;
            float scale = level == 3 ? zoom.zoomScaleAlt2 : level == 2 ? zoom.zoomScaleAlt : zoom.zoomScaleNormal;
            UIDragHandler drag;
            if (target == zoom.transform && zoom.TryGetComponent(out drag))
            {
                drag.SetHomeScale(scale);
                scale *= drag.DockScaleRatio;
            }

            target.localScale = new Vector3(scale, scale, target.localScale.z);
        }

        public static void ClearSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private static void UseGameSprites()
        {
            LoadedMod mod = ModLoader.Mods.Find(m => m.Manifest.ID == ModIds.Id);
            if (mod == null)
            {
                return;
            }

            var wanted = new HashSet<string>(SpriteKeys);
            Sprite[] loaded = Resources.FindObjectsOfTypeAll<Sprite>();
            for (int i = 0; i < loaded.Length; i++)
            {
                Sprite sprite = loaded[i];
                if (sprite != null && wanted.Contains(sprite.name))
                {
                    mod.PaperSprites[sprite.name] = sprite;
                }
            }
        }
    }
}