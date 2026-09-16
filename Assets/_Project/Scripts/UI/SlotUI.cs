using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Одна карточка слота: спрайт машины, имя, ⭐ для бонусной, кнопка «Взять».
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

        private int _slotIndex;
        private Action<int> _onClick;

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
            bool hasCar = slot.currentCar != null;

            if (carImage != null)
            {
                carImage.sprite = hasCar ? slot.currentCar.sprite : null;
                carImage.enabled = hasCar;
            }

            if (nameText != null)
                nameText.text = hasCar ? slot.currentCar.displayName : "...";

            if (bonusMark != null)
                bonusMark.SetActive(hasCar && slot.isBonus);

            if (refillText != null)
            {
                if (hasCar)
                    refillText.text = "";
                else
                {
                    float left = Mathf.Max(0f, slot.refillDelay - slot.timeSinceEmpty);
                    refillText.text = $"{left:F1} с";
                }
            }

            if (background != null)
            {
                if (!hasCar) background.color = emptyColor;
                else if (slot.isBonus) background.color = bonusColor;
                else background.color = normalColor;
            }

            if (clickButton != null)
                clickButton.interactable = hasCar && canTake;
        }
    }
}