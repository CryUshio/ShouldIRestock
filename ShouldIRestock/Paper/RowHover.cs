using UnityEngine;
using UnityEngine.EventSystems;

namespace ShouldIRestock
{
    public sealed class RowHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string TagId;

        public void OnPointerEnter(PointerEventData eventData)
        {
            TagHighlighter.SetHover(TagId);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TagHighlighter.ClearHover(TagId);
        }
    }
}
