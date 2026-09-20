using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Одна карточка машины в коллекции.
    /// Три состояния: отремонтирована / открыта / закрыта.
    /// </summary>
    public class CollectionCardUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Image background;
        [SerializeField] private Image carImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private GameObject checkMark;

        [Header("Цвета")]
        [SerializeField] private Color repairedColor = new Color(0.4f, 0.9f, 0.4f, 0.25f);
        [SerializeField] private Color unlockedColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);

        [Header("Затемнение силуэта")]
        [SerializeField] private Color lockedSpriteTint = new Color(0.2f, 0.2f, 0.2f, 1f);

        private CarData _car;

        public CarData Car => _car;

        public void Bind(CarData car)
        {
            _car = car;
        }

        public void Refresh()
        {
            if (_car == null) return;

            bool repaired = CollectionManager.Instance != null &&
                            CollectionManager.Instance.IsRepaired(_car);
            bool unlocked = CollectionManager.Instance != null &&
                            CollectionManager.Instance.IsUnlocked(_car);

            // Имя машины
            if (nameText != null)
            {
                nameText.text = unlocked ? _car.displayName : "???";
                nameText.color = unlocked ? _car.RarityColor : new Color(0.5f, 0.5f, 0.5f);
            }

            // Спрайт
            if (carImage != null)
            {
                bool hasSprite = _car.sprite != null;
                carImage.sprite = hasSprite ? _car.sprite : null;
                carImage.enabled = hasSprite;
                carImage.color = unlocked ? Color.white : lockedSpriteTint;
            }

            // Фон
            if (background != null)
            {
                if (repaired) background.color = repairedColor;
                else if (unlocked) background.color = unlockedColor;
                else background.color = lockedColor;
            }

            // Замок / галочка
            if (lockIcon != null) lockIcon.SetActive(!unlocked);
            if (checkMark != null) checkMark.SetActive(repaired);

            // Статус-текст
            if (statusText != null)
            {
                if (repaired)
                {
                    statusText.text = "Отремонтирована";
                    statusText.color = new Color(0.4f, 0.9f, 0.4f);
                }
                else if (unlocked)
                {
                    statusText.text = "Открыта";
                    statusText.color = new Color(1f, 0.9f, 0.4f);
                }
                else
                {
                    int left = CollectionManager.Instance != null
                        ? CollectionManager.Instance.HowManyLeftToUnlock(_car.rarity)
                        : 0;
                    statusText.text = $"Ещё {left} машин";
                    statusText.color = new Color(0.6f, 0.6f, 0.6f);
                }
            }
        }
    }
}