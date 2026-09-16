using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>Одна строка магазина: иконка, имя, цена, кнопка «Купить».</summary>
    public class ShopItemUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Button buyButton;

        [Header("Цвета")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color cantAffordColor = new Color(1f, 0.5f, 0.5f, 0.3f);

        private PartData _part;
        private Action<PartData> _onBuy;

        public PartData Part => _part;

        public void Bind(PartData part, Action<PartData> onBuy)
        {
            _part = part;
            _onBuy = onBuy;

            if (iconImage != null) iconImage.sprite = part.icon;
            if (nameText != null) nameText.text = part.displayName;

            int price = ShopManager.Instance != null ? ShopManager.Instance.GetBuyPrice(part) : 0;
            if (priceText != null) priceText.text = $"${price}";

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => _onBuy?.Invoke(_part));
            }
        }

        public void RefreshVisual(int haveCount, bool canAfford)
        {
            if (countText != null) countText.text = haveCount > 0 ? $"×{haveCount}" : "";
            if (background != null) background.color = canAfford ? normalColor : cantAffordColor;
            if (buyButton != null) buyButton.interactable = canAfford;
        }
    }
}