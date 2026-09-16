using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>Один слот заказа.</summary>
    [Serializable]
    public class SlotData
    {
        public CarData currentCar;
        public bool isBonus;
        public bool isInRepair;         // сейчас в гараже
        public float timeSinceEmpty;    // секунд пустует
        public float refillDelay;       // через сколько секунд приедет новая
        public float bonusTimeLeft;     // сколько осталось у бонусной машины (сек)
    }

    /// <summary>
    /// Управляет слотами заказов. 3 старт, максимум 6.
    /// Бонусные машины живут ограниченное время (по ТЗ — 3 минуты).
    /// </summary>
    public class SlotManager : MonoBehaviour
    {
        public static SlotManager Instance { get; private set; }

        [Header("Ссылки")]
        [Tooltip("Реестр машин. Перетащи CarDatabase.asset")]
        [SerializeField] private CarDatabase carDatabase;

        [Header("Настройки")]
        [SerializeField] private int startSlots = 3;
        [SerializeField] private int maxSlots = 6;
        [SerializeField] private float refillDelayMin = 1f;
        [SerializeField] private float refillDelayMax = 2f;

        [Tooltip("Сколько живёт бонусная машина в слоте (сек). По ТЗ — 180")]
        [SerializeField] private float bonusTimeSeconds = 180f;

        [Header("Слоты (не трогай руками)")]
        [SerializeField] private List<SlotData> slots = new List<SlotData>();

        public IReadOnlyList<SlotData> Slots => slots;
        public int MaxSlots => maxSlots;
        public float BonusTimeSeconds => bonusTimeSeconds;

        public event Action OnSlotsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (carDatabase == null)
            {
                Debug.LogError("[SlotManager] CarDatabase не назначен в Inspector!");
                return;
            }

            InitializeSlots();
        }

        private void InitializeSlots()
        {
            while (slots.Count < startSlots) slots.Add(new SlotData());

            for (int i = 0; i < slots.Count; i++)
                if (slots[i].currentCar == null && !slots[i].isInRepair)
                    FillSlot(i);
        }

        private void Update()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];

                // Пустой слот — считаем до приезда новой машины
                if (s.currentCar == null && !s.isInRepair)
                {
                    s.timeSinceEmpty += Time.deltaTime;
                    if (s.timeSinceEmpty >= s.refillDelay) FillSlot(i);
                    continue;
                }

                // Бонусная машина — считаем до отъезда
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
        }

        private CarData PickRandomCar()
        {
            if (carDatabase == null || carDatabase.allCars == null || carDatabase.allCars.Length == 0)
                return null;
            var pool = carDatabase.allCars;
            return pool[UnityEngine.Random.Range(0, pool.Length)];
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
        }

        /// <summary>Забрать машину в гараж. Возвращает CarData.</summary>
        public CarData TakeCar(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return null;
            var car = slots[slotIndex].currentCar;
            if (car == null) return null;

            MakeEmpty(slotIndex);
            return car;
        }

        /// <summary>Завершить заказ и освободить слот.</summary>
        public void ReleaseSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slots.Count) return;
            MakeEmpty(slotIndex);
        }

        /// <summary>Апгрейд: +1 слот. Возвращает true при успехе.</summary>
        public bool TryAddSlot()
        {
            if (slots.Count >= maxSlots) return false;
            slots.Add(new SlotData
            {
                refillDelay = UnityEngine.Random.Range(refillDelayMin, refillDelayMax)
            });
            OnSlotsChanged?.Invoke();
            return true;
        }

        // ===== ТЕСТЫ =====
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