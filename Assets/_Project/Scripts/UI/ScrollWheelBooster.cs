using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AutoMechanic.UI
{
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

            var delta = eventData.scrollDelta;
            delta.y *= wheelMultiplier;
            eventData.scrollDelta = delta;

            _scrollRect.OnScroll(eventData);
        }
    }
}   