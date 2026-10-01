using System;
using UnityEngine;
using AutoMechanic.Core;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Апгрейды гаража. Данные в SaveManager.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        [Header("Цены слотов (по порядку покупки)")]
        [SerializeField] private int[] slotCosts = { 800, 1200, 1800, 2500, 3500 };

        [Header("Цены постов ремонта")]
        public int[] bayCosts = { 2500, 5000 };

        [Header("Лимиты")]
        [SerializeField] private int startSlots = 3;
        [SerializeField] private int maxSlots = 8;
        [SerializeField] private int startBays = 1;
        [SerializeField] private int maxBays = 3;

        public int ExtraSlotsBought => SaveManager.Data.upgSlots;
        public int ExtraBaysBought => SaveManager.Data.upgBays;

        public int CurrentBays => startBays + ExtraBaysBought;

        public bool CanBuyMoreSlots =>
            SlotManager.Instance != null &&
            SlotManager.Instance.Slots.Count < maxSlots &&
            ExtraSlotsBought < slotCosts.Length;

        public bool CanBuyMoreBays =>
            CurrentBays < maxBays &&
            ExtraBaysBought < bayCosts.Length;

        public int NextSlotCost
        {
            get
            {
                int idx = ExtraSlotsBought;
                if (idx < 0 || idx >= slotCosts.Length) return -1;
                return slotCosts[idx];
            }
        }

        public int NextBayCost
        {
            get
            {
                int idx = ExtraBaysBought;
                if (idx < 0 || idx >= bayCosts.Length) return -1;
                return bayCosts[idx];
            }
        }

        public event Action OnUpgradesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SaveManager.OnDataReloaded += HandleDataReloaded;
            _ = SaveManager.Data;
        }

        private void OnDestroy()
        {
            SaveManager.OnDataReloaded -= HandleDataReloaded;
        }

        private void Start()
        {
            ApplyAllUpgrades();
        }

        private void HandleDataReloaded()
        {
            ApplyAllUpgrades();
            OnUpgradesChanged?.Invoke();
        }

        public void ApplyAllUpgrades()
        {
            if (SlotManager.Instance != null)
            {
                int want = ExtraSlotsBought;
                int current = SlotManager.Instance.Slots.Count;
                int haveExtra = current - startSlots;
                int toAdd = want - haveExtra;
                for (int i = 0; i < toAdd; i++)
                    SlotManager.Instance.TryAddSlot();
            }

            if (GarageManager.Instance != null)
                GarageManager.Instance.SetMaxConcurrentRepairs(CurrentBays);

            OnUpgradesChanged?.Invoke();
        }

        public bool TryBuySlotUpgrade()
        {
            if (!CanBuyMoreSlots) { Debug.Log("[UpgradeManager] Максимум слотов"); return false; }
            if (EconomyManager.Instance == null) return false;

            int cost = NextSlotCost;
            if (!EconomyManager.Instance.Spend(cost)) return false;

            SaveManager.Data.upgSlots += 1;
            SaveManager.Save();

            SlotManager.Instance.TryAddSlot();
            Debug.Log($"[UpgradeManager] Куплен слот за ${cost}");
            OnUpgradesChanged?.Invoke();
            return true;
        }

        public bool TryBuyBayUpgrade()
        {
            if (!CanBuyMoreBays) { Debug.Log("[UpgradeManager] Максимум постов"); return false; }
            if (EconomyManager.Instance == null) return false;

            int cost = NextBayCost;
            if (!EconomyManager.Instance.Spend(cost)) return false;

            SaveManager.Data.upgBays += 1;
            SaveManager.Save();

            if (GarageManager.Instance != null)
                GarageManager.Instance.SetMaxConcurrentRepairs(CurrentBays);

            Debug.Log($"[UpgradeManager] Куплен пост за ${cost}");
            OnUpgradesChanged?.Invoke();
            return true;
        }

        [ContextMenu("ТЕСТ: сбросить апгрейды")]
        private void TestReset()
        {
            SaveManager.Data.upgSlots = 0;
            SaveManager.Data.upgBays = 0;
            SaveManager.Save();
            Debug.Log("[UpgradeManager] Апгрейды сброшены");
        }

        [ContextMenu("ТЕСТ: купить слот")]
        private void TestBuySlot() => TryBuySlotUpgrade();

        [ContextMenu("ТЕСТ: купить пост")]
        private void TestBuyBay() => TryBuyBayUpgrade();
    }
}
