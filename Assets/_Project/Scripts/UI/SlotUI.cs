using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Одна карточка слота: спрайт машины, имя, ⭐ для бонусной, кнопка «Взять».
    /// Бонусные машины показывают обратный отсчёт.
    /// </summary>
    public class SlotUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Image background;
        [SerializeField] private Image carImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject bonusMark;
        [SerializeField] private TMP_Text refillText;
        [SerializeField] private Button clickButton;

        [Header("Цвета")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color bonusColor = new Color(1f, 0.85f, 0.3f, 0.25f);
        [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

        [Header("Цвета таймера")]
        [SerializeField] private Color timerNormal = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color timerDanger = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color timerRefill = new Color(0.7f, 0.7f, 0.7f);

        private int _slotIndex;
        private Action<int> _onClick;
        private SlotData _slot;

        public int SlotIndex => _slotIndex;

        public void Bind(int slotIndex, Action<int> onClick)
        {
            _slotIndex = slotIndex;
            _onClick = onClick;

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => _onClick?.Invoke(_slotIndex));
            }
        }

        /// <summary>Обновить визуал карточки под текущее состояние слота.</summary>
        public void Refresh(SlotData slot, bool canTake)
        {
            _slot = slot;
            if (_slot == null) return;

            bool hasCar = slot.currentCar != null;

            // Спрайт
            if (carImage != null)
            {
                bool hasSprite = hasCar && slot.currentCar.sprite != null;
                carImage.sprite = hasSprite ? slot.currentCar.sprite : null;
                carImage.enabled = hasSprite;
            }

            // Имя
            if (nameText != null)
                nameText.text = hasCar ? slot.currentCar.displayName : "...";

            // ★
            if (bonusMark != null)
                bonusMark.SetActive(hasCar && slot.isBonus);

            // Цвет фона
            if (background != null)
            {
                if (!hasCar) background.color = emptyColor;
                else if (slot.isBonus) background.color = bonusColor;
                else background.color = normalColor;
            }

            // Кнопка
            if (clickButton != null)
                clickButton.interactable = hasCar && canTake;

            // Таймер — на этот кадр
            UpdateTimerText();
        }

        private void Update()
        {
            // Таймер должен тикать каждый кадр, а не только при OnSlotsChanged
            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            if (refillText == null || _slot == null) return;

            if (_slot.currentCar != null)
            {
                if (_slot.isBonus)
                {
                    float t = Mathf.Max(0f, _slot.bonusTimeLeft);
                    int min = Mathf.FloorToInt(t / 60f);
                    int sec = Mathf.FloorToInt(t % 60f);
                    refillText.text = $"★ {min}:{sec:00}";
                    refillText.color = t <= 30f ? timerDanger : timerNormal;
                }
                else
                {
                    refillText.text = "";
                }
            }
            else
            {
                float left = Mathf.Max(0f, _slot.refillDelay - _slot.timeSinceEmpty);
                refillText.text = $"{left:F1} с";
                refillText.color = timerRefill;
            }
        }
    }
}