using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Коллекция: отслеживает уникальные отремонтированные машины.
    /// Хранит прогресс в PlayerPrefs.
    /// </summary>
    public class CollectionManager : MonoBehaviour
    {
        public static CollectionManager Instance { get; private set; }

        private const string KeyPrefix = "am_repaired_";
        private const string KeyCount = "am_repaired_count";

        [Header("Ссылки")]
        [Tooltip("Реестр машин. Перетащи CarDatabase.asset")]
        [SerializeField] private CarDatabase carDatabase;

        private readonly HashSet<string> _repairedIds = new HashSet<string>();

        public int RepairedCount => _repairedIds.Count;

        public event Action OnCollectionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public bool IsRepaired(string carId)
        {
            return !string.IsNullOrEmpty(carId) && _repairedIds.Contains(carId);
        }

        public bool IsRepaired(CarData car) => car != null && IsRepaired(car.id);

        public void MarkRepaired(CarData car)
        {
            if (car == null || string.IsNullOrEmpty(car.id)) return;
            if (_repairedIds.Contains(car.id)) return;

            _repairedIds.Add(car.id);
            Save();

            Debug.Log($"[CollectionManager] Новая машина отремонтирована: {car.displayName}. Всего: {RepairedCount}");

            CheckUnlocks();
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

        public List<CarData> GetUnlockedCars()
        {
            var result = new List<CarData>();
            if (carDatabase == null || carDatabase.allCars == null) return result;

            foreach (var car in carDatabase.allCars)
            {
                if (car == null) continue;
                if (IsUnlocked(car)) result.Add(car);
            }
            return result;
        }

        [ContextMenu("Сбросить коллекцию")]
        public void ResetCollection()
        {
            foreach (var id in _repairedIds)
                PlayerPrefs.DeleteKey(KeyPrefix + id);

            _repairedIds.Clear();
            PlayerPrefs.SetInt(KeyCount, 0);
            PlayerPrefs.Save();

            Debug.Log("[CollectionManager] Коллекция сброшена");
            OnCollectionChanged?.Invoke();
        }

        private void CheckUnlocks()
        {
            if (carDatabase == null || carDatabase.allCars == null) return;

            foreach (var car in carDatabase.allCars)
            {
                if (car == null) continue;
                int required = CarData.GetRequiredProgress(car.rarity);
                if (RepairedCount == required && required > 0)
                    Debug.Log($"[CollectionManager] 🔓 Открыта новая машина: {car.displayName} ({car.rarity})");
            }
        }

        private void Save()
        {
            foreach (var id in _repairedIds)
                PlayerPrefs.SetInt(KeyPrefix + id, 1);

            PlayerPrefs.SetInt(KeyCount, _repairedIds.Count);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            _repairedIds.Clear();
            if (carDatabase == null || carDatabase.allCars == null) return;

            foreach (var car in carDatabase.allCars)
            {
                if (car == null) continue;
                if (PlayerPrefs.GetInt(KeyPrefix + car.id, 0) == 1)
                    _repairedIds.Add(car.id);
            }

            Debug.Log($"[CollectionManager] Загружено отремонтированных: {_repairedIds.Count}");
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
                    sb.AppendLine($"  {mark} {car.displayName} [{car.rarity}] (нужно {CarData.GetRequiredProgress(car.rarity)})");
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