using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Панель апгрейдов. Открывается кнопкой «Апгрейды».
    /// </summary>
    public class UpgradePanelUI : MonoBehaviour
    {
        /// <summary>Событие: открыта ли панель апгрейдов (true = открыта).</summary>
        public static event Action<bool> OnUpgradeToggled;

        [Header("Ссылки")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private UpgradeCardUI slotCard;
        [SerializeField] private UpgradeCardUI doubleCard;
        [SerializeField] private Button closeButton;

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (rootPanel != null) rootPanel.SetActive(false);

            if (slotCard != null)
                slotCard.Bind("+1 слот заказа", "Больше машин одновременно на выбор", OnBuySlot);
            if (doubleCard != null)
                doubleCard.Bind("Двойной ремонт", "Возможность ремонтировать 2 машины сразу", OnBuyDouble);

            Subscribe();
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradesChanged -= Refresh;
            if (SlotManager.Instance != null)
                SlotManager.Instance.OnSlotsChanged -= Refresh;
        }

        private void Subscribe()
        {
            if (UpgradeManager.Instance != null)
            {
                UpgradeManager.Instance.OnUpgradesChanged -= Refresh;
                UpgradeManager.Instance.OnUpgradesChanged += Refresh;
            }
            if (SlotManager.Instance != null)
            {
                SlotManager.Instance.OnSlotsChanged -= Refresh;
                SlotManager.Instance.OnSlotsChanged += Refresh;
            }
        }

        public void Open()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            Refresh();
            OnUpgradeToggled?.Invoke(true);
        }

        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            OnUpgradeToggled?.Invoke(false);
        }

        public void Toggle()
        {
            if (rootPanel == null) return;
            if (rootPanel.activeSelf) Close(); else Open();
        }

        private void OnBuySlot()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.TryBuySlotUpgrade();
            Refresh();
        }

        private void OnBuyDouble()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.TryBuyDoubleRepair();
            Refresh();
        }

        public void Refresh()
        {
            if (UpgradeManager.Instance == null) return;

            int money = EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0;

            if (moneyText != null)
                moneyText.text = $"${money}";

            bool slotBought = !UpgradeManager.Instance.CanBuyMoreSlots;
            if (slotCard != null)
                slotCard.Refresh(slotBought, UpgradeManager.Instance.SlotUpgradeCost, money);

            if (doubleCard != null)
                doubleCard.Refresh(UpgradeManager.Instance.HasDoubleRepair, UpgradeManager.Instance.DoubleRepairCost, money);
        }
    }
}