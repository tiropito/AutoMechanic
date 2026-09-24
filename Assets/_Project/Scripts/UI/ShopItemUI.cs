using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;
using DG.Tweening;

namespace AutoMechanic.UI
{
    public class ShopItemUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Button buyButton;

        [Header("Продажа")]
        [SerializeField] private Button sellButton;
        [SerializeField] private TMP_Text sellButtonText;

        [Header("Цвета")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color cantAffordColor = new Color(1f, 0.5f, 0.5f, 0.3f);

        private PartData _part;
        private Action<PartData> _onBuy;
        private Action<PartData> _onSell;

        private Tween _highlightTween;

        public PartData Part => _part;

        public void Bind(PartData part, Action<PartData> onBuy, Action<PartData> onSell)
        {
            _part = part;
            _onBuy = onBuy;
            _onSell = onSell;

            if (iconImage != null) iconImage.sprite = part.icon;

            if (nameText != null)
            {
                nameText.text = part.displayName;
                nameText.color = part.RarityColor;
            }

            if (descriptionText != null)
                descriptionText.text = string.IsNullOrEmpty(part.description) ? "—" : part.description;

            int buyPrice = ShopManager.Instance != null ? ShopManager.Instance.GetBuyPrice(part) : 0;
            if (priceText != null) priceText.text = $"${buyPrice}";

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => _onBuy?.Invoke(_part));
            }

            if (sellButton != null)
            {
                sellButton.onClick.RemoveAllListeners();
                sellButton.onClick.AddListener(() => _onSell?.Invoke(_part));
            }

            if (sellButtonText != null)
                sellButtonText.text = $"Продать ${part.SellPrice}";
        }

        public void RefreshVisual(int haveCount, bool canAfford)
        {
            if (countText != null) countText.text = haveCount > 0 ? $"×{haveCount}" : "";
            if (background != null) background.color = canAfford ? normalColor : cantAffordColor;
            if (buyButton != null) buyButton.interactable = canAfford;
            if (sellButton != null) sellButton.interactable = haveCount > 0;
        }

        // ==================== ПОДСВЕТКА ====================

        public void Highlight(float duration = 5f)
        {
            if (background == null) return;

            _highlightTween?.Kill();

            Color baseColor = background.color;
            Color highlightColor = new Color(1f, 0.9f, 0.3f, 0.7f);

            _highlightTween = background.DOColor(highlightColor, 0.4f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .OnKill(() => { if (background != null) background.color = baseColor; });

            DOVirtual.DelayedCall(duration, () =>
            {
                _highlightTween?.Kill();
                if (background != null) background.color = baseColor;
            });
        }

        private void OnDestroy()
        {
            _highlightTween?.Kill();
        }
    }
}