using System;
using UnityEngine;

namespace AutoMechanic.Core
{
    /// <summary>
    /// Управляет деньгами игрока. Singleton — один на всю сцену.
    /// Сохраняет баланс в PlayerPrefs (работает в WebGL).
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        private const string SaveKeyMoney = "am_money";
        private const int StartMoney = 200; // из ТЗ, раздел 11

        [SerializeField] private int currentMoney = StartMoney;

        /// <summary>Текущий баланс</summary>
        public int Money => currentMoney;

        /// <summary>Вызывается при каждом изменении баланса. Аргумент — новый баланс.</summary>
        public event Action<int> OnMoneyChanged;

        private void Awake()
        {
            // Singleton: если уже есть — уничтожаем дубликат
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Load();
        }

        /// <summary>Начислить деньги (ремонт, заказ, продажа детали)</summary>
        public void Add(int amount)
        {
            if (amount <= 0) return;
            currentMoney += amount;
            Save();
            OnMoneyChanged?.Invoke(currentMoney);
        }

        /// <summary>Попытаться списать деньги. Возвращает false, если не хватает.</summary>
        public bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (currentMoney < amount) return false;

            currentMoney -= amount;
            Save();
            OnMoneyChanged?.Invoke(currentMoney);
            return true;
        }

        /// <summary>Хватает ли денег на покупку</summary>
        public bool CanAfford(int amount) => currentMoney >= amount;

        /// <summary>Сбросить прогресс (для отладки)</summary>
        [ContextMenu("Сбросить деньги до стартовых")]
        public void ResetToStart()
        {
            currentMoney = StartMoney;
            Save();
            OnMoneyChanged?.Invoke(currentMoney);
            Debug.Log($"[EconomyManager] Деньги сброшены до ${StartMoney}");
        }

        private void Save()
        {
            PlayerPrefs.SetInt(SaveKeyMoney, currentMoney);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            currentMoney = PlayerPrefs.GetInt(SaveKeyMoney, StartMoney);
        }

        // ===== ТЕСТ: кликни правой кнопкой на компоненте в Play Mode =====
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