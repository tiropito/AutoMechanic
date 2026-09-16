using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Гараж: хранит машину, которую игрок сейчас ремонтирует.
    /// Машина приходит из SlotManager.TakeCar().
    /// </summary>
    public class GarageManager : MonoBehaviour
    {
        public static GarageManager Instance { get; private set; }

        [Header("Настройки")]
        [Tooltip("Одновременно можно чинить столько машин (1 или 2 после апгрейда)")]
        [SerializeField] private int maxConcurrentRepairs = 1;

        [Header("Текущие машины (не трогай руками)")]
        [SerializeField] private List<RepairSession> sessions = new List<RepairSession>();

        public IReadOnlyList<RepairSession> Sessions => sessions;
        public int MaxConcurrentRepairs => maxConcurrentRepairs;

        /// <summary>Можно ли взять ещё одну машину в ремонт</summary>
        public bool HasFreeBay => sessions.Count < maxConcurrentRepairs;

        public event Action OnSessionsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Взять машину из слота. Возвращает true, если успешно.
        /// slotIndex — какой слот забрать.
        /// </summary>
        public bool TakeCarFromSlot(int slotIndex)
        {
            if (SlotManager.Instance == null)
            {
                Debug.LogError("[GarageManager] SlotManager не найден на сцене!");
                return false;
            }

            if (!HasFreeBay)
            {
                Debug.Log("[GarageManager] Все посты заняты");
                return false;
            }

            var car = SlotManager.Instance.TakeCar(slotIndex);
            if (car == null)
            {
                Debug.Log($"[GarageManager] Слот {slotIndex} пуст");
                return false;
            }

            var session = new RepairSession
            {
                car = car,
                slotIndex = slotIndex,
                state = RepairState.NotDiagnosed,
                brokenDownList = new List<BreakdownData>()
            };

            sessions.Add(session);
            OnSessionsChanged?.Invoke();
            Debug.Log($"[GarageManager] Взял в ремонт: {car.displayName}");
            return true;
        }

        /// <summary>Завершить ремонт и освободить пост.</summary>
        public void CompleteRepair(int sessionIndex)
        {
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return;
            var s = sessions[sessionIndex];
            SlotManager.Instance?.ReleaseSlot(s.slotIndex);
            sessions.RemoveAt(sessionIndex);
            OnSessionsChanged?.Invoke();
        }

        /// <summary>Апгрейд: двойной ремонт</summary>
        public void EnableDoubleRepair()
        {
            maxConcurrentRepairs = 2;
            Debug.Log("[GarageManager] Двойной ремонт активирован");
        }

        // ===== ТЕСТЫ =====
        [ContextMenu("ТЕСТ: взять из слота 0")]
        private void TestTake0() => TakeCarFromSlot(0);

        [ContextMenu("ТЕСТ: взять из слота 1")]
        private void TestTake1() => TakeCarFromSlot(1);

        [ContextMenu("ТЕСТ: завершить ремонт 0")]
        private void TestComplete0() => CompleteRepair(0);

        [ContextMenu("ТЕСТ: включить двойной ремонт")]
        private void TestDouble() => EnableDoubleRepair();

        [ContextMenu("ТЕСТ: показать сессии")]
        private void TestDump()
        {
            var sb = new System.Text.StringBuilder("[GarageManager]\n");
            sb.AppendLine($"  MaxConcurrent: {maxConcurrentRepairs}");
            if (sessions.Count == 0)
                sb.AppendLine("  (пусто)");
            foreach (var s in sessions)
                sb.AppendLine($"  {s.car.displayName} | state={s.state} | слот {s.slotIndex}");
            Debug.Log(sb.ToString());
        }
    }

    /// <summary>Состояние ремонта одной машины.</summary>
    public enum RepairState
    {
        NotDiagnosed,   // ещё не делали диагностику
        Diagnosed,      // диагностика пройдена, известны поломки
        Completed       // все поломки устранены
    }

    /// <summary>Одна сессия ремонта.</summary>
    [Serializable]
    public class RepairSession
    {
        public CarData car;
        public int slotIndex;
        public RepairState state;
        public List<BreakdownData> brokenDownList;   // известные поломки (после диагностики)
    }
}