using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using DG.Tweening;

namespace AutoMechanic.UI
{
    public class DamagePopupUI : MonoBehaviour
    {
        public static DamagePopupUI Instance { get; private set; }

        [Header("Ссылки")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image partIcon;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text partNameText;
        [SerializeField] private TMP_Text partPriceText;
        [SerializeField] private Button openShopButton;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (openShopButton != null) openShopButton.onClick.AddListener(OpenShop);
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        public void Show(BreakdownData breakdown)
        {
            if (breakdown == null) return;

            var part = (breakdown.requiredParts != null && breakdown.requiredParts.Length > 0)
                ? breakdown.requiredParts[0] : null;

            if (part == null) return;

            if (titleText != null) titleText.text = "НУЖНА ДЕТАЛЬ";
            if (partNameText != null)
            {
                partNameText.text = part.displayName;
                partNameText.color = part.RarityColor;
            }
            if (partPriceText != null) partPriceText.text = $"${part.buyPrice}";

            if (partIcon != null)
            {
                bool hasIcon = part.icon != null;
                partIcon.sprite = hasIcon ? part.icon : null;
                partIcon.gameObject.SetActive(hasIcon);
            }

            if (rootPanel != null) rootPanel.SetActive(true);

            var rt = rootPanel.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one * 0.7f;
                rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.15f);
            }
        }

        public void Hide()
        {
            if (rootPanel == null) return;

            var rt = rootPanel.GetComponent<RectTransform>();
            if (rt != null) rt.DOScale(0.7f, 0.18f).SetEase(Ease.InBack);

            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.18f).OnComplete(() =>
                {
                    if (rootPanel != null) rootPanel.SetActive(false);
                });
            }
        }

        private void OpenShop()
        {
            Hide();
            var shop = FindObjectOfType<ShopPanelUI>(true);   // ← true
            if (shop != null) shop.Open();
        }
    }
}