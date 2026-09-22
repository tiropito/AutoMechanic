using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using DG.Tweening;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Один таб переключения между машинами в гараже.
    /// Название окрашено по редкости машины. При выборе — bounce.
    /// </summary>
    public class SessionTabUI : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;

        [Header("Цвета")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.6f, 1f, 0.5f);

        [Header("Анимация выбора")]
        [SerializeField] private float bounceScale = 1.12f;
        [SerializeField] private float bounceDuration = 0.35f;

        private int _index;
        private Action<int> _onClick;
        private bool _wasSelected;
        private Tween _bounceTween;

        public void Bind(int index, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClick?.Invoke(_index));
            }
        }

        public void Refresh(CarData car, bool isSelected)
        {
            if (label != null)
            {
                label.text = car != null ? car.displayName : "Машина";
                label.color = car != null ? car.RarityColor : Color.white;
            }

            if (background != null)
                background.color = isSelected ? selectedColor : normalColor;

            // Bounce при переходе в "выбран"
            if (isSelected && !_wasSelected)
                PlaySelectBounce();

            _wasSelected = isSelected;
        }

        public void Refresh(string carName, bool isSelected)
        {
            if (label != null)
            {
                label.text = carName;
                label.color = Color.white;
            }

            if (background != null)
                background.color = isSelected ? selectedColor : normalColor;

            if (isSelected && !_wasSelected)
                PlaySelectBounce();

            _wasSelected = isSelected;
        }

        private void PlaySelectBounce()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            _bounceTween?.Kill();
            rt.localScale = Vector3.one;

            _bounceTween = rt.DOPunchScale(Vector3.one * (bounceScale - 1f), bounceDuration, 6, 0.7f);
        }

        private void OnDestroy()
        {
            _bounceTween?.Kill();
        }
    }
}