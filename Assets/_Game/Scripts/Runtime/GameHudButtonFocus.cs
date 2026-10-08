using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Tycoon
{
    // Chỉ nhận hover/chọn để thao tác kéo vẫn truyền tới danh sách cuộn.
    sealed class HudButtonFocus : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public Button Target;
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (Target && Target.interactable && EventSystem.current)
                EventSystem.current.SetSelectedGameObject(Target.gameObject);
        }
        public void OnSelect(BaseEventData eventData)
        {
            var scroll = GetComponentInParent<ScrollRect>();
            if (!scroll || !scroll.content || !scroll.viewport) return;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, transform);
            var view = scroll.viewport.rect;
            float move = bounds.max.y > view.yMax ? view.yMax - bounds.max.y :
            bounds.min.y < view.yMin ? view.yMin - bounds.min.y : 0;
            scroll.content.anchoredPosition += new Vector2(0, move);
        }
    }
}
