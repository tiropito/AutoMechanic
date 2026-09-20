using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    [Serializable]
    public class SlotData
    {
        public CarData currentCar;
        public bool isBonus;
        public bool isInRepair;
        public float timeSinceEmpty;
        public float refillDelay;
        public float bonusTimeLeft;
    }

    public class SlotManager : MonoBehaviour
    {
        public static SlotManager Instance { get; private set; }

        private const string KeySlotCount = "am_slot_count";
        private const string KeySlotPrefix = "am_slot_";

        [Header("Ссылки")]
        [SerializeField] private CarDatabase carDatabase;

        [Header("Настройки")]
        [SerializeField] private int startSlots = 3;
        [SerializeField] private int maxSlots = 6;
        [SerializeField] private float refillDelayMin = 1f;
        [SerializeField] private float refillDelayMax = 2f;
        [SerializeField] private float bonusTimeSeconds = 180f;

        [Header("Слоты (не трогай руками)")]
        [SerializeField] private List<SlotData> slots = new List<SlotData>();

        public IReadOnlyList<SlotData> Slots => slots;
        public int MaxSlots => maxSlots;
        public float BonusTimeSeconds => bonusTimeSeconds;

        public event Action OnSlotsChanged;

        private int _lastSaveFrame = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (carDatabase == null)
            {
                Debug.LogError("[SlotManager] CarDatabase не назначен!");
                return;
            }

            Load();
        }

        private void OnApplicationQuit() { Save(); }
        private void OnApplicationPause(bool pause) { if (pause) Save(); }

        private void Update()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];

                if (s.currentCar == null && !s.isInRepair)
                {
                    s.timeSinceEmpty += Time.deltaTime;
                    if (s.timeSinceEmpty >= s.refillDelay) FillSlot(i);
                    continue;
                }

                if (s.currentCar != null && s.isBonus && !s.isInRepair)
                {
                    s.bonusTimeLeft -= Time.deltaTime;
                    if (s.bonusTimeLeft <= 0f)
                    {
                        Debug.Log($"[SlotManager] Бонусная {s.currentCar.displayName} уехала — время вышло");
                        MakeEmpty(i);
                    }
                }
            }
        }

        private void FillSlot(int index)
        {
            var car = PickRandomCar();
            slots[index].currentCar = car;
            slots[index].isBonus = car != null && car.isBonus;
            slots[index].timeSinceEmpty = 0f;
            slots[index].refillDelay = UnityEngine.Random.Range(refillDelayMin, refillDelayMax);
            slots[index].bonusTimeLeft = slots[index].isBonus ? bonusTimeSeconds : 0f;

            OnSlotsChanged?.Invoke();
            Save();
        }

        private CarData PickRandomCar()
        {
            if (carDatabase == null || carDatabase.allCars == null || carDatabase.allCars.Length == 0)
                return null;

            var pool = new List<CarData>();
            foreach (var car in carDatabase.allCars)
            {
                if (car == null) continue;
                if (CollectionManager.Instance != null && !CollectionManager.Instance.IsUnlocked(car))
                    continue;
                pool.Add(car);
            }

            if (pool.Count == 0) return null;
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        private void MakeEmpty(int index)
        {
            var s = slots[index];
            s.currentCar = null;
            s.isBonus = false;
            s.isInRepair = false;
            s.timeSinceEmpty = 0f;
            s.bonusTimeLeft = 0f;
            s.refillDelay = UnityEngine.Random.Range(refillDelayMin, refillDelayMax);
            OnSlotsChanged?.Invoke();
            Save();
        }

        public CarData TakeCar(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return null;
            var car = slots[slotIndex].currentCar;
            if (car == null) return null;

            MakeEmpty(slotIndex);
            return car;
        }

        public void ReleaseSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            MakeEmpty(slotIndex);
        }

        public bool TryAddSlot()
        {
            if (slots.Count >= maxSlots) return false;
            slots.Add(new SlotData
            {
                refillDelay = UnityEngine.Random.Range(refillDelayMin, refillDelayMax)
            });
            OnSlotsChanged?.Invoke();
            Save();
            return true;
        }

        // ==================== СОХРАНЕНИЕ ====================

        public void Save()
        {
            if (_lastSaveFrame == Time.frameCount) return;
            _lastSaveFrame = Time.frameCount;

            PlayerPrefs.SetInt(KeySlotCount, slots.Count);

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                string p = KeySlotPrefix + i + "_";

                PlayerPrefs.SetString(p + "car", s.currentCar != null ? s.currentCar.id : "");
                PlayerPrefs.SetInt(p + "bonus", s.isBonus ? 1 : 0);
                PlayerPrefs.SetFloat(p + "bonus_time", s.bonusTimeLeft);
                PlayerPrefs.SetFloat(p + "empty_time", s.timeSinceEmpty);
                PlayerPrefs.SetFloat(p + "refill", s.refillDelay);
            }

            PlayerPrefs.Save();
        }

        public void Load()
        {
            slots.Clear();

            int count = PlayerPrefs.GetInt(KeySlotCount, 0);

            if (count <= 0)
            {
                while (slots.Count < startSlots) slots.Add(new SlotData());
                for (int i = 0; i < slots.Count; i++) FillSlot(i);
                Save();
                return;
            }

            for (int i = 0; i < count; i++)
            {
                string p = KeySlotPrefix + i + "_";
                var data = new SlotData
                {
                    isBonus = PlayerPrefs.GetInt(p + "bonus", 0) == 1,
                    bonusTimeLeft = PlayerPrefs.GetFloat(p + "bonus_time", 0f),
                    timeSinceEmpty = PlayerPrefs.GetFloat(p + "empty_time", 0f),
                    refillDelay = PlayerPrefs.GetFloat(p + "refill", UnityEngine.Random.Range(refillDelayMin, refillDelayMax))
                };

                string carId = PlayerPrefs.GetString(p + "car", "");
                if (!string.IsNullOrEmpty(carId))
                    data.currentCar = FindCarById(carId);

                if (data.currentCar != null && data.isBonus && data.bonusTimeLeft < 5f)
                {
                    data.currentCar = null;
                    data.isBonus = false;
                    data.bonusTimeLeft = 0f;
                }

                slots.Add(data);
            }

            for (int i = 0; i < slots.Count; i++)
                if (slots[i].currentCar == null && !slots[i].isInRepair)
                    FillSlot(i);

            Debug.Log($"[SlotManager] Загружено слотов: {slots.Count}");
            OnSlotsChanged?.Invoke();
        }

        private CarData FindCarById(string id)
        {
            if (carDatabase == null || carDatabase.allCars == null) return null;
            foreach (var c in carDatabase.allCars)
                if (c != null && c.id == id) return c;
            return null;
        }

        [ContextMenu("ТЕСТ: сбросить сохранение слотов")]
        public void ResetSave()
        {
            int count = PlayerPrefs.GetInt(KeySlotCount, 0);
            for (int i = 0; i < count; i++)
            {
                string p = KeySlotPrefix + i + "_";
                PlayerPrefs.DeleteKey(p + "car");
                PlayerPrefs.DeleteKey(p + "bonus");
                PlayerPrefs.DeleteKey(p + "bonus_time");
                PlayerPrefs.DeleteKey(p + "empty_time");
                PlayerPrefs.DeleteKey(p + "refill");
            }
            PlayerPrefs.DeleteKey(KeySlotCount);
            PlayerPrefs.Save();
            Debug.Log("[SlotManager] Сохранение слотов сброшено. Перезапусти Play");
        }

        [ContextMenu("ТЕСТ: показать слоты")]
        private void TestDump()
        {
            var sb = new System.Text.StringBuilder("[SlotManager]\n");
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                var name = s.currentCar != null ? s.currentCar.displayName : "(пусто)";
                var bonus = s.isBonus ? $" ★ {s.bonusTimeLeft:F1}с" : "";
                sb.AppendLine($"  [{i}] {name}{bonus} timer={s.timeSinceEmpty:F1}/{s.refillDelay:F1}");
            }
            Debug.Log(sb.ToString());
        }

        [ContextMenu("ТЕСТ: освободить слот 0")]
        private void TestRelease0() => ReleaseSlot(0);

        [ContextMenu("ТЕСТ: +1 слот")]
        private void TestAddSlot()
        {
            if (TryAddSlot()) Debug.Log($"[SlotManager] Слотов теперь: {slots.Count}");
            else Debug.Log("[SlotManager] Максимум достигнут");
        }

        [ContextMenu("ТЕСТ: сделать слот 0 бонусным (30 сек)")]
        private void TestForceBonus()
        {
            if (slots.Count == 0 || slots[0].currentCar == null) return;
            slots[0].isBonus = true;
            slots[0].bonusTimeLeft = 30f;
            OnSlotsChanged?.Invoke();
            Debug.Log("[SlotManager] Слот 0 стал бонусным на 30 сек");
        }
    }
}