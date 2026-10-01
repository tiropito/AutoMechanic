using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Data;
using AutoMechanic.Core;

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

    /// <summary>
    /// Слоты заказов. Все данные хранятся в SaveManager.Data (YG2.saves).
    /// </summary>
    public class SlotManager : MonoBehaviour
    {
        public static SlotManager Instance { get; private set; }

        [Header("Ссылки")]
        [SerializeField] private CarDatabase carDatabase;

        [Header("Настройки")]
        [SerializeField] private int startSlots = 3;
        [SerializeField] private int maxSlots = 6;
        [SerializeField] private float refillDelayMin = 1f;
        [SerializeField] private float refillDelayMax = 2f;
        [SerializeField] private float bonusTimeSeconds = 30f;

        [Header("Антиповтор")]
        [SerializeField] private bool avoidDuplicates = true;

        [Header("Слоты (не трогай руками)")]
        [SerializeField] private List<SlotData> slots = new List<SlotData>();

        public IReadOnlyList<SlotData> Slots => slots;
        public int MaxSlots => maxSlots;
        public float BonusTimeSeconds => bonusTimeSeconds;

        public event Action OnSlotsChanged;

        private bool _collectionSubscribed;
        private bool _started;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (carDatabase == null)
                Debug.LogError("[SlotManager] CarDatabase не назначен!");

            SaveManager.OnDataReloaded += HandleDataReloaded;
            LoadFromSave();
        }

        private void OnDestroy()
        {
            SaveManager.OnDataReloaded -= HandleDataReloaded;
        }

        private void HandleDataReloaded()
        {
            Debug.Log("[SlotManager] Данные перезагружены — обновляю слоты");
            LoadFromSave();
        }

        private void Start()
        {
            _started = true;
            TrySubscribeCollection();
            ValidateSlotsAgainstCollection();
            FillEmptySlotsSafely();
        }

        private void OnEnable()
        {
            if (_started) TrySubscribeCollection();
        }

        private void OnDisable()
        {
            UnsubscribeCollection();
        }

        // ==================== ПОДПИСКА НА КОЛЛЕКЦИЮ ====================

        private void TrySubscribeCollection()
        {
            if (_collectionSubscribed) return;
            if (CollectionManager.Instance == null) return;

            CollectionManager.Instance.OnCollectionChanged += OnCollectionChanged;
            _collectionSubscribed = true;
        }

        private void UnsubscribeCollection()
        {
            if (!_collectionSubscribed) return;
            if (CollectionManager.Instance != null)
                CollectionManager.Instance.OnCollectionChanged -= OnCollectionChanged;
            _collectionSubscribed = false;
        }

        private void OnCollectionChanged()
        {
            ValidateSlotsAgainstCollection();
            FillEmptySlotsSafely();
        }

        // ==================== ВАЛИДАЦИЯ ====================

        private void ValidateSlotsAgainstCollection()
        {
            if (CollectionManager.Instance == null) return;

            bool changed = false;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s == null || s.currentCar == null) continue;
                if (CollectionManager.Instance.IsUnlocked(s.currentCar)) continue;

                Debug.Log($"[SlotManager] Слот {i}: {s.currentCar.id} не разблокирован — освобождаю");
                s.currentCar = null;
                s.isBonus = false;
                s.bonusTimeLeft = 0f;
                s.timeSinceEmpty = 0f;
                s.refillDelay = UnityEngine.Random.Range(refillDelayMin, refillDelayMax);
                changed = true;
            }

            if (changed)
            {
                OnSlotsChanged?.Invoke();
                SaveToSave();
            }
        }

        private void FillEmptySlotsSafely()
        {
            if (CollectionManager.Instance == null)
            {
                Debug.LogWarning("[SlotManager] CollectionManager не готов — пропускаю заполнение");
                return;
            }

            bool changed = false;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s.currentCar != null) continue;
                if (s.isInRepair) continue;
                if (s.timeSinceEmpty < s.refillDelay) continue;

                FillSlot(i);
                changed = true;
            }

            if (changed) SaveToSave();
        }

        // ==================== ОСНОВНОЙ ЦИКЛ ====================

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
                        Debug.Log($"[SlotManager] Бонусная {s.currentCar.displayName} уехала");
                        MakeEmpty(i);
                    }
                }
            }
        }

        private void FillSlot(int index)
        {
            var car = PickRandomCar(index);
            slots[index].currentCar = car;
            slots[index].isBonus = car != null && car.isBonus;
            slots[index].timeSinceEmpty = 0f;
            slots[index].refillDelay = UnityEngine.Random.Range(refillDelayMin, refillDelayMax);
            slots[index].bonusTimeLeft = slots[index].isBonus ? bonusTimeSeconds : 0f;

            if (slots[index].isBonus && AudioManager.Instance != null)
                AudioManager.Instance.PlayBonus();

            OnSlotsChanged?.Invoke();
            SaveToSave();
        }

        private CarData PickRandomCar(int forSlotIndex)
        {
            if (carDatabase == null || carDatabase.allCars == null || carDatabase.allCars.Length == 0)
                return null;

            var pool = new List<CarData>();

            if (CollectionManager.Instance == null)
            {
                Debug.LogWarning("[SlotManager] CollectionManager не готов — только Basic-машины");
                foreach (var car in carDatabase.allCars)
                {
                    if (car == null) continue;
                    if (car.rarity != CarRarity.Basic) continue;
                    pool.Add(car);
                }
            }
            else
            {
                foreach (var car in carDatabase.allCars)
                {
                    if (car == null) continue;
                    if (!CollectionManager.Instance.IsUnlocked(car)) continue;
                    pool.Add(car);
                }
            }

            if (pool.Count == 0)
            {
                Debug.LogWarning("[SlotManager] Пул машин пуст");
                return null;
            }

            if (!avoidDuplicates)
                return pool[UnityEngine.Random.Range(0, pool.Count)];

            var usedIds = new HashSet<string>();
            for (int i = 0; i < slots.Count; i++)
            {
                if (i == forSlotIndex) continue;
                if (slots[i] == null) continue;
                if (slots[i].currentCar == null) continue;
                if (slots[i].isInRepair) continue;
                usedIds.Add(slots[i].currentCar.id);
            }

            if (usedIds.Count >= pool.Count)
                return pool[UnityEngine.Random.Range(0, pool.Count)];

            var uniquePool = new List<CarData>();
            foreach (var car in pool)
            {
                if (car == null) continue;
                if (!usedIds.Contains(car.id)) uniquePool.Add(car);
            }

            if (uniquePool.Count > 0)
                return uniquePool[UnityEngine.Random.Range(0, uniquePool.Count)];

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
            SaveToSave();
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
            SaveToSave();
            return true;
        }

        // ==================== СОХРАНЕНИЕ ====================

        private void SaveToSave()
        {
            var data = SaveManager.Data;
            data.slotCount = slots.Count;

            data.slotCars.Clear();
            data.slotBonus.Clear();
            data.slotBonusTime.Clear();
            data.slotEmptyTime.Clear();
            data.slotRefill.Clear();

            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                data.slotCars.Add(s.currentCar != null ? s.currentCar.id : "");
                data.slotBonus.Add(s.isBonus);
                data.slotBonusTime.Add(s.bonusTimeLeft);
                data.slotEmptyTime.Add(s.timeSinceEmpty);
                data.slotRefill.Add(s.refillDelay);
            }

            SaveManager.Save();
        }

        private void LoadFromSave()
        {
            slots.Clear();
            var data = SaveManager.Data;

            int count = data.slotCount;
            if (count <= 0) count = startSlots;

            for (int i = 0; i < count; i++)
            {
                var slot = new SlotData();

                if (i < data.slotCars.Count)
                {
                    string carId = data.slotCars[i];
                    if (!string.IsNullOrEmpty(carId))
                        slot.currentCar = FindCarById(carId);

                    if (i < data.slotBonus.Count) slot.isBonus = data.slotBonus[i];
                    if (i < data.slotBonusTime.Count) slot.bonusTimeLeft = data.slotBonusTime[i];
                    if (i < data.slotEmptyTime.Count) slot.timeSinceEmpty = data.slotEmptyTime[i];
                    if (i < data.slotRefill.Count) slot.refillDelay = data.slotRefill[i];
                }

                if (slot.refillDelay <= 0f)
                    slot.refillDelay = UnityEngine.Random.Range(refillDelayMin, refillDelayMax);

                // Валидация: машина должна быть разблокирована
                if (slot.currentCar != null && CollectionManager.Instance != null
                    && !CollectionManager.Instance.IsUnlocked(slot.currentCar))
                {
                    Debug.Log($"[SlotManager] Слот {i}: {slot.currentCar.id} не разблокирован");
                    slot.currentCar = null;
                    slot.isBonus = false;
                    slot.bonusTimeLeft = 0f;
                }

                // Бонусная машина почти кончилась — освобождаем
                if (slot.currentCar != null && slot.isBonus && slot.bonusTimeLeft < 5f)
                {
                    slot.currentCar = null;
                    slot.isBonus = false;
                    slot.bonusTimeLeft = 0f;
                }

                slots.Add(slot);
            }

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

        // ==================== ТЕСТЫ ====================

        [ContextMenu("ТЕСТ: сбросить слоты")]
        public void ResetSlots()
        {
            var data = SaveManager.Data;
            data.slotCount = startSlots;
            data.slotCars.Clear();
            data.slotBonus.Clear();
            data.slotBonusTime.Clear();
            data.slotEmptyTime.Clear();
            data.slotRefill.Clear();
            SaveManager.Save();

            slots.Clear();
            for (int i = 0; i < startSlots; i++)
                slots.Add(new SlotData { refillDelay = UnityEngine.Random.Range(refillDelayMin, refillDelayMax) });

            Debug.Log("[SlotManager] Слоты сброшены. Перезапусти Play");
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

        [ContextMenu("ТЕСТ: пересобрать слоты")]
        private void TestRebuild()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].currentCar = null;
                slots[i].isInRepair = false;
                slots[i].timeSinceEmpty = 0f;
                FillSlot(i);
            }
            Debug.Log("[SlotManager] Слоты пересобраны");
        }

        [ContextMenu("ТЕСТ: освободить слот 0")]
        private void TestRelease0() => ReleaseSlot(0);

        [ContextMenu("ТЕСТ: +1 слот")]
        private void TestAddSlot()
        {
            if (TryAddSlot()) Debug.Log($"[SlotManager] Слотов: {slots.Count}");
            else Debug.Log("[SlotManager] Максимум достигнут");
        }

        [ContextMenu("ТЕСТ: сделать слот 0 бонусным (30 сек)")]
        private void TestForceBonus()
        {
            if (slots.Count == 0 || slots[0].currentCar == null) return;
            slots[0].isBonus = true;
            slots[0].bonusTimeLeft = 30f;
            OnSlotsChanged?.Invoke();
            SaveToSave();
        }

        [ContextMenu("ТЕСТ: какие машины разблокированы")]
        private void TestShowUnlocked()
        {
            if (carDatabase == null || carDatabase.allCars == null) return;
            var sb = new System.Text.StringBuilder("[SlotManager] Разблокированные:\n");
            int total = 0;
            foreach (var car in carDatabase.allCars)
            {
                if (car == null) continue;
                bool unlocked = CollectionManager.Instance != null && CollectionManager.Instance.IsUnlocked(car);
                sb.AppendLine($"  {(unlocked ? "✅" : "🔒")} {car.displayName} [{car.rarity}]");
                if (unlocked) total++;
            }
            sb.AppendLine($"Итого: {total}");
            Debug.Log(sb.ToString());
        }
    }
}
