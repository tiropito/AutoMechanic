using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Увеличивает чувствительность колёсика мыши для ScrollRect.
    /// Нужно для WebGL, где штатный скролл работает туго.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollWheelBooster : MonoBehaviour, IScrollHandler
    {
        [Tooltip("Множитель скорости прокрутки")]
        [SerializeField] private float wheelMultiplier = 3f;

        private ScrollRect _scrollRect;

        private void Awake()
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (_scrollRect == null) return;

            // Применяем множитель к прокрутке
            var delta = eventData.scrollDelta;
            delta.y *= wheelMultiplier;
            eventData.scrollDelta = delta;

            _scrollRect.OnScroll(eventData);
        }
    }
}