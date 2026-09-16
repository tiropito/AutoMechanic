using System;
using UnityEngine;
using AutoMechanic.Core;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Управляет апгрейдами гаража. Покупаются один раз, сохраняются в PlayerPrefs.
    /// При старте игры — восстанавливает эффекты (слоты, двойной ремонт).
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        private const string KeyExtraSlots = "am_upg_slots";      // сколько слотов докуплено
        private const string KeyDoubleRepair = "am_upg_double";   // 1 = куплен

        [Header("Цены")]
        [SerializeField] private int slotUpgradeCost = 300;
        [SerializeField] private int doubleRepairCost = 800;

        [Header("Лимиты")]
        [Tooltip("Максимум слотов (по ТЗ 6)")]
        [SerializeField] private int maxSlots = 6;

        public int SlotUpgradeCost => slotUpgradeCost;
        public int DoubleRepairCost => doubleRepairCost;

        /// <summary>Куплен ли двойной ремонт.</summary>
        public bool HasDoubleRepair => PlayerPrefs.GetInt(KeyDoubleRepair, 0) == 1;

        /// <summary>Сколько слотов докуплено (0..3).</summary>
        public int ExtraSlotsBought => PlayerPrefs.GetInt(KeyExtraSlots, 0);

        /// <summary>Можно ли ещё докупить слот.</summary>
        public bool CanBuyMoreSlots => SlotManager.Instance != null &&
                                       SlotManager.Instance.Slots.Count < maxSlots;

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

        /// <summary>Применяет все купленные апгрейды к менеджерам. Вызывается при старте.</summary>
        public void ApplyAllUpgrades()
        {
            // Слоты
            if (SlotManager.Instance != null)
            {
                int want = ExtraSlotsBought;
                int current = SlotManager.Instance.Slots.Count;
                int startSlots = 3; // по ТЗ старт 3
                int haveExtra = current - startSlots;
                int toAdd = want - haveExtra;
                for (int i = 0; i < toAdd; i++)
                    SlotManager.Instance.TryAddSlot();
            }

            // Двойной ремонт
            if (HasDoubleRepair && GarageManager.Instance != null)
                GarageManager.Instance.EnableDoubleRepair();

            OnUpgradesChanged?.Invoke();
        }

        // ==================== ПОКУПКА ====================

        /// <summary>Купить +1 слот за $300.</summary>
        public bool TryBuySlotUpgrade()
        {
            if (!CanBuyMoreSlots)
            {
                Debug.Log("[UpgradeManager] Максимум слотов уже достигнут");
                return false;
            }

            if (EconomyManager.Instance == null)
            {
                Debug.LogError("[UpgradeManager] Нет EconomyManager");
                return false;
            }

            if (!EconomyManager.Instance.Spend(slotUpgradeCost))
            {
                Debug.Log($"[UpgradeManager] Не хватает денег на слот: нужно ${slotUpgradeCost}");
                return false;
            }

            PlayerPrefs.SetInt(KeyExtraSlots, ExtraSlotsBought + 1);
            PlayerPrefs.Save();

            SlotManager.Instance.TryAddSlot();
            Debug.Log($"[UpgradeManager] Куплен слот за ${slotUpgradeCost}. Теперь слотов: {SlotManager.Instance.Slots.Count}");

            OnUpgradesChanged?.Invoke();
            return true;
        }

        /// <summary>Купить двойной ремонт за $800.</summary>
        public bool TryBuyDoubleRepair()
        {
            if (HasDoubleRepair)
            {
                Debug.Log("[UpgradeManager] Двойной ремонт уже куплен");
                return false;
            }

            if (EconomyManager.Instance == null)
            {
                Debug.LogError("[UpgradeManager] Нет EconomyManager");
                return false;
            }

            if (!EconomyManager.Instance.Spend(doubleRepairCost))
            {
                Debug.Log($"[UpgradeManager] Не хватает денег: нужно ${doubleRepairCost}");
                return false;
            }

            PlayerPrefs.SetInt(KeyDoubleRepair, 1);
            PlayerPrefs.Save();

            if (GarageManager.Instance != null)
                GarageManager.Instance.EnableDoubleRepair();

            Debug.Log($"[UpgradeManager] Куплен двойной ремонт за ${doubleRepairCost}");
            OnUpgradesChanged?.Invoke();
            return true;
        }

        // ==================== ТЕСТЫ ====================
        [ContextMenu("ТЕСТ: сбросить апгрейды")]
        private void TestReset()
        {
            PlayerPrefs.DeleteKey(KeyExtraSlots);
            PlayerPrefs.DeleteKey(KeyDoubleRepair);
            PlayerPrefs.Save();
            Debug.Log("[UpgradeManager] Апгрейды сброшены (перезапусти Play)");
        }

        [ContextMenu("ТЕСТ: купить слот")]
        private void TestBuySlot() => TryBuySlotUpgrade();

        [ContextMenu("ТЕСТ: купить двойной ремонт")]
        private void TestBuyDouble() => TryBuyDoubleRepair();
    }
}