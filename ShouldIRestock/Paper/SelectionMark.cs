using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShouldIRestock
{
    public static class SelectionMark
    {
        private const string Node = "SIR_Selected";
        private static bool searched;
        private static Sprite checkSprite;

        public static void Apply(PaperUI.Binding binding, bool selected)
        {
            Transform found = binding.go.transform.Find(Node);
            GameObject mark = found != null ? found.gameObject : Create(binding);
            if (mark == null)
            {
                return;
            }

            RectTransform rect = mark.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(-5f, 0f);
                rect.sizeDelta = new Vector2(14f, 14f);
            }

            mark.SetActive(selected);
        }

        private static GameObject Create(PaperUI.Binding binding)
        {
            ResolveSprite();
            var go = new GameObject(Node);
            go.transform.SetParent(binding.go.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(-5f, 0f);
            rect.sizeDelta = new Vector2(14f, 14f);

            if (checkSprite != null)
            {
                var image = go.AddComponent<Image>();
                image.sprite = checkSprite;
                image.color = Color.black;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else
            {
                var text = go.AddComponent<TextMeshProUGUI>();
                if (binding.text != null)
                {
                    text.font = binding.text.font;
                }

                text.text = "√";
                text.fontSize = 10f;
                text.color = Color.black;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
            }

            go.SetActive(false);
            return go;
        }

        private static void ResolveSprite()
        {
            if (searched)
            {
                return;
            }

            CustomUIManager ui = CustomUIManager.Instance;
            if (ui == null)
            {
                return;
            }

            searched = true;
            string[] keys = { "check", "checkmark", "icon_check", "icons_check", "tick", "confirm", "accept" };
            for (int i = 0; i < keys.Length && checkSprite == null; i++)
            {
                checkSprite = ui.GetSprite(keys[i], true);
            }
        }
    }
}
