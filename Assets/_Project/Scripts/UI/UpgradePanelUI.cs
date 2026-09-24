using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    public class UpgradePanelUI : MonoBehaviour
    {
        public static event Action<bool> OnUpgradeToggled;

        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TMP_Text moneyText;

        [Header("Карточки")]
        [SerializeField] private UpgradeCardUI slotCard;
        [SerializeField] private UpgradeCardUI garage2Card;
        [SerializeField] private UpgradeCardUI garage3Card;

        [SerializeField] private Button closeButton;

        public bool IsOpen => rootPanel != null && rootPanel.activeSelf;
        
        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (rootPanel != null) rootPanel.SetActive(false);

            if (slotCard != null)
                slotCard.Bind("+1 слот заказа", "Больше машин на выбор одновременно", OnBuySlot);

            if (garage2Card != null)
                garage2Card.Bind("Второй пост ремонта", "Ремонтируй 2 машины одновременно", OnBuyGarage2);

            if (garage3Card != null)
                garage3Card.Bind("Третий пост ремонта", "Ремонтируй 3 машины одновременно", OnBuyGarage3);

            Subscribe();

            if (FullScreenPanelManager.Instance != null)
                FullScreenPanelManager.Instance.RegisterUpgrade(this);
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
            // Закрываем другие полноэкранные панели
            if (FullScreenPanelManager.Instance != null)
                FullScreenPanelManager.Instance.CloseAllExcept(this);

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

        private void OnBuyGarage2()
        {
            if (UpgradeManager.Instance == null) return;
            if (UpgradeManager.Instance.ExtraBaysBought >= 1) return;
            UpgradeManager.Instance.TryBuyBayUpgrade();
            Refresh();
        }

        private void OnBuyGarage3()
        {
            if (UpgradeManager.Instance == null) return;
            if (UpgradeManager.Instance.ExtraBaysBought >= 2) return;
            UpgradeManager.Instance.TryBuyBayUpgrade();
            Refresh();
        }

        public void Refresh()
        {
            if (UpgradeManager.Instance == null) return;

            int money = EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0;
            if (moneyText != null) moneyText.text = $"${money}";

            var um = UpgradeManager.Instance;

            // === Слоты ===
            if (slotCard != null)
            {
                bool bought = !um.CanBuyMoreSlots;
                int price = um.NextSlotCost;
                if (price < 0) price = 0;

                if (bought) slotCard.Refresh(true, 0, money);
                else slotCard.Refresh(false, price, money);
            }

            // === Второй пост ===
            if (garage2Card != null)
            {
                bool bought = um.ExtraBaysBought >= 1;
                int price = (um.bayCosts != null && um.bayCosts.Length > 0) ? um.bayCosts[0] : 0;

                if (bought) garage2Card.Refresh(true, 0, money);
                else garage2Card.Refresh(false, price, money);
            }

            // === Третий пост ===
            if (garage3Card != null)
            {
                bool bought = um.ExtraBaysBought >= 2;
                int price = (um.bayCosts != null && um.bayCosts.Length > 1) ? um.bayCosts[1] : 0;

                if (bought) garage3Card.Refresh(true, 0, money);
                else
                {
                    if (um.ExtraBaysBought < 1)
                        garage3Card.Refresh(true, 0, 0);
                    else
                        garage3Card.Refresh(false, price, money);
                }
            }
        }
    }
}