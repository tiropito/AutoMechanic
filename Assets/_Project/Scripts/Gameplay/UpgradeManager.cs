using System;
using UnityEngine;
using AutoMechanic.Core;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Управляет апгрейдами гаража: слоты заказов и посты ремонта.
    /// Прогрессивные цены: каждая покупка дороже.
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        private const string KeyExtraSlots = "am_upg_slots";      // сколько слотов докуплено
        private const string KeyExtraBays = "am_upg_bays";        // сколько постов докуплено

        [Header("Цены слотов (по порядку покупки)")]
        [Tooltip("3→4 = 800, 4→5 = 1200, 5→6 = 1800, 6→7 = 2500, 7→8 = 3500")]
        [SerializeField] private int[] slotCosts = { 800, 1200, 1800, 2500, 3500 };

        [Header("Цены постов ремонта (по порядку покупки)")]
        [Tooltip("1→2 = 2500, 2→3 = 5000")]
        public int[] bayCosts = { 2500, 5000 };

        [Header("Лимиты")]
        [SerializeField] private int startSlots = 3;
        [SerializeField] private int maxSlots = 8;
        [SerializeField] private int startBays = 1;
        [SerializeField] private int maxBays = 3;

        /// <summary>Сколько слотов докуплено (0..N).</summary>
        public int ExtraSlotsBought => PlayerPrefs.GetInt(KeyExtraSlots, 0);

        /// <summary>Сколько постов докуплено (0..N).</summary>
        public int ExtraBaysBought => PlayerPrefs.GetInt(KeyExtraBays, 0);

        /// <summary>Текущий уровень постов: 1 + ExtraBaysBought.</summary>
        public int CurrentBays => startBays + ExtraBaysBought;

        /// <summary>Можно ли ещё докупить слот.</summary>
        public bool CanBuyMoreSlots =>
            SlotManager.Instance != null &&
            SlotManager.Instance.Slots.Count < maxSlots &&
            ExtraSlotsBought < slotCosts.Length;

        /// <summary>Можно ли ещё докупить пост.</summary>
        public bool CanBuyMoreBays =>
            CurrentBays < maxBays &&
            ExtraBaysBought < bayCosts.Length;

        /// <summary>Цена следующего слота (или -1, если нельзя).</summary>
        public int NextSlotCost
        {
            get
            {
                int idx = ExtraSlotsBought;
                if (idx < 0 || idx >= slotCosts.Length) return -1;
                return slotCosts[idx];
            }
        }

        /// <summary>Цена следующего поста (или -1, если нельзя).</summary>
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
        }

        private void Start()
        {
            ApplyAllUpgrades();
        }

        /// <summary>Применяет все купленные апгрейды к менеджерам при старте.</summary>
        public void ApplyAllUpgrades()
        {
            // Слоты
            if (SlotManager.Instance != null)
            {
                int want = ExtraSlotsBought;
                int current = SlotManager.Instance.Slots.Count;
                int haveExtra = current - startSlots;
                int toAdd = want - haveExtra;
                for (int i = 0; i < toAdd; i++)
                    SlotManager.Instance.TryAddSlot();
            }

            // Посты
            if (GarageManager.Instance != null)
            {
                int target = CurrentBays;
                GarageManager.Instance.SetMaxConcurrentRepairs(target);
            }

            OnUpgradesChanged?.Invoke();
        }

        // ==================== ПОКУПКА ====================

        /// <summary>Купить следующий слот. Цена зависит от количества купленных.</summary>
        public bool TryBuySlotUpgrade()
        {
            if (!CanBuyMoreSlots)
            {
                Debug.Log("[UpgradeManager] Максимум слотов уже достигнут");
                return false;
            }

            if (EconomyManager.Instance == null) return false;

            int cost = NextSlotCost;
            if (!EconomyManager.Instance.Spend(cost))
            {
                Debug.Log($"[UpgradeManager] Не хватает на слот: нужно ${cost}");
                return false;
            }

            PlayerPrefs.SetInt(KeyExtraSlots, ExtraSlotsBought + 1);
            PlayerPrefs.Save();

            SlotManager.Instance.TryAddSlot();
            Debug.Log($"[UpgradeManager] Куплен слот за ${cost}. Всего слотов: {SlotManager.Instance.Slots.Count}");

            OnUpgradesChanged?.Invoke();
            return true;
        }

        /// <summary>Купить следующий пост ремонта (гараж).</summary>
        public bool TryBuyBayUpgrade()
        {
            if (!CanBuyMoreBays)
            {
                Debug.Log("[UpgradeManager] Максимум постов уже достигнут");
                return false;
            }

            if (EconomyManager.Instance == null) return false;

            int cost = NextBayCost;
            if (!EconomyManager.Instance.Spend(cost))
            {
                Debug.Log($"[UpgradeManager] Не хватает на пост: нужно ${cost}");
                return false;
            }

            PlayerPrefs.SetInt(KeyExtraBays, ExtraBaysBought + 1);
            PlayerPrefs.Save();

            if (GarageManager.Instance != null)
                GarageManager.Instance.SetMaxConcurrentRepairs(CurrentBays);

            Debug.Log($"[UpgradeManager] Куплен пост за ${cost}. Всего постов: {CurrentBays}");

            OnUpgradesChanged?.Invoke();
            return true;
        }

        // ==================== ТЕСТЫ ====================

        [ContextMenu("ТЕСТ: сбросить апгрейды")]
        private void TestReset()
        {
            PlayerPrefs.DeleteKey(KeyExtraSlots);
            PlayerPrefs.DeleteKey(KeyExtraBays);
            PlayerPrefs.Save();
            Debug.Log("[UpgradeManager] Апгрейды сброшены (перезапусти Play)");
        }

        [ContextMenu("ТЕСТ: купить слот")]
        private void TestBuySlot() => TryBuySlotUpgrade();

        [ContextMenu("ТЕСТ: купить пост")]
        private void TestBuyBay() => TryBuyBayUpgrade();
    }
}