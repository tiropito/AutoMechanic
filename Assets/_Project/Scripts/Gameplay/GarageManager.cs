using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Гараж: хранит машины, которые игрок сейчас ремонтирует.
    /// Поддерживает переключение между несколькими машинами (после апгрейда).
    /// </summary>
    public class GarageManager : MonoBehaviour
    {
        public static GarageManager Instance { get; private set; }

        [Header("Настройки")]
        [Tooltip("Одновременно можно чинить столько машин (1 или 2 после апгрейда)")]
        [SerializeField] private int maxConcurrentRepairs = 1;

        [Header("Текущие машины (не трогай руками)")]
        [SerializeField] private List<RepairSession> sessions = new List<RepairSession>();

        [Tooltip("Индекс активной машины в гараже")]
        [SerializeField] private int currentSessionIndex = 0;

        public IReadOnlyList<RepairSession> Sessions => sessions;
        public int MaxConcurrentRepairs => maxConcurrentRepairs;
        public int CurrentSessionIndex => currentSessionIndex;

        /// <summary>Можно ли взять ещё одну машину в ремонт</summary>
        public bool HasFreeBay => sessions.Count < maxConcurrentRepairs;

        /// <summary>Активная сессия (или null, если машин нет)</summary>
        public RepairSession CurrentSession =>
            (sessions.Count > 0 && currentSessionIndex >= 0 && currentSessionIndex < sessions.Count)
            ? sessions[currentSessionIndex]
            : null;

        /// <summary>Изменение списка сессий (взяли / завершили / слотов стало больше)</summary>
        public event Action OnSessionsChanged;

        /// <summary>Смена активной машины (клик по табу)</summary>
        public event Action<int> OnCurrentSessionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ==================== ВЗЯТЬ МАШИНУ ====================

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
                brokenDownList = new List<BreakdownData>(),
                fixedList = new List<BreakdownData>()
            };

            sessions.Add(session);

            // Новая машина автоматически становится активной
            currentSessionIndex = sessions.Count - 1;

            OnSessionsChanged?.Invoke();
            OnCurrentSessionChanged?.Invoke(currentSessionIndex);

            Debug.Log($"[GarageManager] Взял в ремонт: {car.displayName}");
            return true;
        }

        // ==================== ЗАВЕРШИТЬ РЕМОНТ ====================

        /// <summary>Завершить ремонт и освободить пост.</summary>
        public void CompleteRepair(int sessionIndex)
        {
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return;

            var s = sessions[sessionIndex];

            // ВАЖНО: слот уже освобождён в TakeCarFromSlot.
            // Повторно вызывать SlotManager.ReleaseSlot НЕЛЬЗЯ — 
            // он обнулит уже приехавшую новую машину.

            sessions.RemoveAt(sessionIndex);

            // Корректируем активный индекс
            if (currentSessionIndex >= sessions.Count)
                currentSessionIndex = Mathf.Max(0, sessions.Count - 1);
            if (currentSessionIndex < 0)
                currentSessionIndex = 0;

            OnSessionsChanged?.Invoke();
            OnCurrentSessionChanged?.Invoke(currentSessionIndex);

            Debug.Log($"[GarageManager] Заказ завершён. Осталось машин: {sessions.Count}");
        }

        // ==================== ПЕРЕКЛЮЧЕНИЕ ====================

        /// <summary>Выбрать активную машину в гараже.</summary>
        public void SelectSession(int index)
        {
            if (index < 0 || index >= sessions.Count) return;
            if (currentSessionIndex == index) return;

            currentSessionIndex = index;
            OnCurrentSessionChanged?.Invoke(currentSessionIndex);
            Debug.Log($"[GarageManager] Выбрана машина #{index}: {sessions[index].car.displayName}");
        }

        /// <summary>Следующая машина (по кругу).</summary>
        public void NextSession()
        {
            if (sessions.Count <= 1) return;
            int next = (currentSessionIndex + 1) % sessions.Count;
            SelectSession(next);
        }

        /// <summary>Предыдущая машина (по кругу).</summary>
        public void PrevSession()
        {
            if (sessions.Count <= 1) return;
            int prev = (currentSessionIndex - 1 + sessions.Count) % sessions.Count;
            SelectSession(prev);
        }

        // ==================== АПГРЕЙДЫ ====================

        /// <summary>Апгрейд: двойной ремонт</summary>
        public void EnableDoubleRepair()
        {
            maxConcurrentRepairs = 2;
            Debug.Log("[GarageManager] Двойной ремонт активирован");
        }

        // ==================== ТЕСТЫ ====================

        [ContextMenu("ТЕСТ: взять из слота 0")]
        private void TestTake0() => TakeCarFromSlot(0);

        [ContextMenu("ТЕСТ: взять из слота 1")]
        private void TestTake1() => TakeCarFromSlot(1);

        [ContextMenu("ТЕСТ: завершить ремонт 0")]
        private void TestComplete0() => CompleteRepair(0);

        [ContextMenu("ТЕСТ: включить двойной ремонт")]
        private void TestDouble() => EnableDoubleRepair();

        [ContextMenu("ТЕСТ: следующая машина")]
        private void TestNext() => NextSession();

        [ContextMenu("ТЕСТ: предыдущая машина")]
        private void TestPrev() => PrevSession();

        [ContextMenu("ТЕСТ: показать сессии")]
        private void TestDump()
        {
            var sb = new System.Text.StringBuilder("[GarageManager]\n");
            sb.AppendLine($"  MaxConcurrent: {maxConcurrentRepairs}");
            sb.AppendLine($"  Active index: {currentSessionIndex}");
            if (sessions.Count == 0)
                sb.AppendLine("  (пусто)");
            for (int i = 0; i < sessions.Count; i++)
            {
                var s = sessions[i];
                string mark = i == currentSessionIndex ? " ← активная" : "";
                sb.AppendLine($"  [{i}] {s.car.displayName} | state={s.state} | слот {s.slotIndex}{mark}");
            }
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

        /// <summary>Все известные поломки (после диагностики).</summary>
        public List<BreakdownData> brokenDownList = new List<BreakdownData>();

        /// <summary>Те поломки, которые уже устранены (✅).</summary>
        public List<BreakdownData> fixedList = new List<BreakdownData>();
    }
}