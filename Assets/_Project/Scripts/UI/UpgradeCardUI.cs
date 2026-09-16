using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Одна карточка апгрейда: иконка, название, описание, цена, кнопка «Купить».
    /// </summary>
    public class UpgradeCardUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyButtonText;

        [Header("Цвета")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color boughtColor = new Color(0.4f, 1f, 0.4f, 0.2f);
        [SerializeField] private Color cantAffordColor = new Color(1f, 0.5f, 0.5f, 0.2f);

        private Action _onBuy;

        public void Bind(string title, string desc, Action onBuy)
        {
            if (titleText != null) titleText.text = title;
            if (descText != null) descText.text = desc;

            _onBuy = onBuy;

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => _onBuy?.Invoke());
            }
        }

        /// <summary>Обновляет визуал в зависимости от состояния (куплено, хватает денег).</summary>
        public void Refresh(bool isBought, int price, int currentMoney)
        {
            if (priceText != null)
                priceText.text = isBought ? "Куплено" : $"${price}";

            bool canAfford = currentMoney >= price;

            if (buyButton != null)
                buyButton.interactable = !isBought && canAfford;

            if (buyButtonText != null)
                buyButtonText.text = isBought ? "✓" : "Купить";

            if (background != null)
            {
                if (isBought) background.color = boughtColor;
                else if (!canAfford) background.color = cantAffordColor;
                else background.color = normalColor;
            }
        }
    }
}