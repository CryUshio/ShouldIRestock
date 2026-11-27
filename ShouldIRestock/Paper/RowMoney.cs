using TMPro;
using UnityEngine;

namespace ShouldIRestock
{
    public static class RowMoney
    {
        private const string Node = "SIR_Value";

        public static void Apply(PaperUI.Binding binding, string value)
        {
            if (binding == null || binding.go == null || binding.text == null)
            {
                return;
            }

            Transform found = binding.go.transform.Find(Node);
            TextMeshProUGUI text = found != null ? found.GetComponent<TextMeshProUGUI>() : Create(binding);
            if (text == null)
            {
                return;
            }

            text.text = value;
            MoneyFormat.Apply(text);
            text.ForceMeshUpdate();
        }

        private static TextMeshProUGUI Create(PaperUI.Binding binding)
        {
            var go = new GameObject(Node);
            go.transform.SetParent(binding.go.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-4f, 0f);
            rect.sizeDelta = new Vector2(72f, 16f);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = binding.text.font;
            text.fontSize = binding.text.fontSize;
            text.fontStyle = binding.text.fontStyle;
            text.color = binding.text.color;
            text.alignment = TextAlignmentOptions.MidlineRight;
            text.richText = true;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }
    }
}
