using System;
using UnityEngine;

namespace AutoMechanic.Core
{
    /// <summary>
    /// Управляет деньгами игрока. Синхронизируется с SaveManager.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        private const int StartMoney = 200;

        public int Money => SaveManager.Data.money;

        public event Action<int> OnMoneyChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Подписка на перезагрузку данных из SDK
            SaveManager.OnDataReloaded += HandleDataReloaded;

            // Форсируем первичную загрузку
            _ = SaveManager.Data;
        }

        private void OnDestroy()
        {
            SaveManager.OnDataReloaded -= HandleDataReloaded;
        }

        private void HandleDataReloaded()
        {
            OnMoneyChanged?.Invoke(Money);
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            SaveManager.Data.money += amount;
            SaveManager.Save();
            OnMoneyChanged?.Invoke(Money);
        }

        public bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (SaveManager.Data.money < amount) return false;

            SaveManager.Data.money -= amount;
            SaveManager.Save();
            OnMoneyChanged?.Invoke(Money);
            return true;
        }

        public bool CanAfford(int amount) => SaveManager.Data.money >= amount;

        [ContextMenu("Сбросить деньги до стартовых")]
        public void ResetToStart()
        {
            SaveManager.Data.money = StartMoney;
            SaveManager.Save();
            OnMoneyChanged?.Invoke(Money);
            Debug.Log($"[EconomyManager] Деньги сброшены до ${StartMoney}");
        }

        // ===== ТЕСТЫ =====

        [ContextMenu("ТЕСТ: +50")]
        private void TestAdd50() => Add(50);

        [ContextMenu("ТЕСТ: -100")]
        private void TestSpend100()
        {
            if (Spend(100)) Debug.Log("[EconomyManager] -$100 ок");
            else Debug.Log("[EconomyManager] Не хватает денег");
        }
    }
}
