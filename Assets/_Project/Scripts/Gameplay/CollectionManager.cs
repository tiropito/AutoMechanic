using System;
using UnityEngine;
using AutoMechanic.Core;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Коллекция отремонтированных машин. Данные в SaveManager.
    /// </summary>
    public class CollectionManager : MonoBehaviour
    {
        public static CollectionManager Instance { get; private set; }

        [Tooltip("Реестр машин. Перетащи CarDatabase.asset")]
        [SerializeField] private CarDatabase carDatabase;

        public int RepairedCount => SaveManager.Data.repairedIds.Count;

        public event Action OnCollectionChanged;

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

        private void HandleDataReloaded()
        {
            OnCollectionChanged?.Invoke();
        }

        public bool IsRepaired(string carId)
        {
            return !string.IsNullOrEmpty(carId) && SaveManager.Data.repairedIds.Contains(carId);
        }

        public bool IsRepaired(CarData car) => car != null && IsRepaired(car.id);

        public void MarkRepaired(CarData car)
        {
            if (car == null || string.IsNullOrEmpty(car.id)) return;
            if (SaveManager.Data.repairedIds.Contains(car.id)) return;

            SaveManager.Data.repairedIds.Add(car.id);
            SaveManager.Save();

            Debug.Log($"[CollectionManager] Новая машина: {car.displayName}. Всего: {RepairedCount}");
            OnCollectionChanged?.Invoke();
        }

        public bool IsUnlocked(CarData car)
        {
            if (car == null) return false;
            int required = CarData.GetRequiredProgress(car.rarity);
            return RepairedCount >= required;
        }

        public int HowManyLeftToUnlock(CarRarity rarity)
        {
            int required = CarData.GetRequiredProgress(rarity);
            return Mathf.Max(0, required - RepairedCount);
        }

        [ContextMenu("Сбросить коллекцию")]
        public void ResetCollection()
        {
            SaveManager.Data.repairedIds.Clear();
            SaveManager.Save();
            Debug.Log("[CollectionManager] Коллекция сброшена");
            OnCollectionChanged?.Invoke();
        }

        [ContextMenu("ТЕСТ: показать коллекцию")]
        private void TestDump()
        {
            var sb = new System.Text.StringBuilder("[CollectionManager]\n");
            sb.AppendLine($"  Отремонтировано: {RepairedCount}");
            if (carDatabase != null && carDatabase.allCars != null)
            {
                foreach (var car in carDatabase.allCars)
                {
                    if (car == null) continue;
                    bool done = IsRepaired(car);
                    bool unlocked = IsUnlocked(car);
                    string mark = done ? "✅" : (unlocked ? "🔓" : "🔒");
                    sb.AppendLine($"  {mark} {car.displayName} [{car.rarity}]");
                }
            }
            Debug.Log(sb.ToString());
        }

        [ContextMenu("ТЕСТ: +1 ремонт первой доступной")]
        private void TestAddOne()
        {
            if (carDatabase == null || carDatabase.allCars == null) return;
            foreach (var car in carDatabase.allCars)
            {
                if (car == null) continue;
                if (!IsRepaired(car)) { MarkRepaired(car); return; }
            }
        }
    }
}
