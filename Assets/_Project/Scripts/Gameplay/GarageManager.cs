using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    public class GarageManager : MonoBehaviour
    {
        public static GarageManager Instance { get; private set; }

        [Header("Настройки")]
        [SerializeField] private int maxConcurrentRepairs = 1;

        [Header("Текущие машины")]
        [SerializeField] private List<RepairSession> sessions = new List<RepairSession>();
        [SerializeField] private int currentSessionIndex = 0;

        public IReadOnlyList<RepairSession> Sessions => sessions;
        public int MaxConcurrentRepairs => maxConcurrentRepairs;
        public int CurrentSessionIndex => currentSessionIndex;
        public bool HasFreeBay => sessions.Count < maxConcurrentRepairs;

        public RepairSession CurrentSession =>
            (sessions.Count > 0 && currentSessionIndex >= 0 && currentSessionIndex < sessions.Count)
            ? sessions[currentSessionIndex]
            : null;

        public event Action OnSessionsChanged;
        public event Action<int> OnCurrentSessionChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool TakeCarFromSlot(int slotIndex)
        {
            if (SlotManager.Instance == null)
            {
                Debug.LogError("[GarageManager] SlotManager не найден!");
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

            GenerateBreakdowns(session);
            sessions.Add(session);
            currentSessionIndex = sessions.Count - 1;

            OnSessionsChanged?.Invoke();
            OnCurrentSessionChanged?.Invoke(currentSessionIndex);

            Debug.Log($"[GarageManager] Взял в ремонт: {car.displayName}");
            return true;
        }

        /// <summary>Заранее назначает поломки. Только те, что соответствуют редкости машины.</summary>
        private void GenerateBreakdowns(RepairSession session)
        {
            if (session == null || session.car == null) return;
            if (session.car.possibleBreakdowns == null || session.car.possibleBreakdowns.Length == 0) return;

            // Максимальная редкость поломки для этой машины
            PartRarity maxAllowed = GetMaxBreakdownRarity(session.car.rarity);

            var pool = new List<BreakdownData>();
            foreach (var bd in session.car.possibleBreakdowns)
            {
                if (bd == null) continue;

                // Пропускаем поломки, которые слишком редкие для этой машины
                var bdRarity = bd.GetRarity();
                if (bdRarity > maxAllowed) continue;

                pool.Add(bd);
            }

            if (pool.Count == 0)
            {
                Debug.LogWarning($"[GarageManager] У машины {session.car.id} нет подходящих поломок (max rarity: {maxAllowed})");
                return;
            }

            int wanted = UnityEngine.Random.Range(1, 4);
            int count = Mathf.Clamp(wanted, 1, pool.Count);

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            for (int i = 0; i < count; i++)
                session.brokenDownList.Add(pool[i]);

            Debug.Log($"[GarageManager] Назначено поломок (заранее): {count} (max rarity: {maxAllowed})");
        }

        /// <summary>Максимальная редкость поломки по тиру машины.</summary>
        private PartRarity GetMaxBreakdownRarity(CarRarity machineRarity)
        {
            switch (machineRarity)
            {
                case CarRarity.Basic:   return PartRarity.Common;
                case CarRarity.Medium:  return PartRarity.Uncommon;
                case CarRarity.Premium: return PartRarity.Rare;
                case CarRarity.Luxury:  return PartRarity.Epic;
                case CarRarity.Secret:  return PartRarity.Epic;
                default:                return PartRarity.Common;
            }
        }

        public void CompleteRepair(int sessionIndex)
        {
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return;

            sessions.RemoveAt(sessionIndex);

            if (currentSessionIndex >= sessions.Count)
                currentSessionIndex = Mathf.Max(0, sessions.Count - 1);
            if (currentSessionIndex < 0)
                currentSessionIndex = 0;

            OnSessionsChanged?.Invoke();
            OnCurrentSessionChanged?.Invoke(currentSessionIndex);

            Debug.Log($"[GarageManager] Заказ завершён. Осталось машин: {sessions.Count}");
        }

        public void SelectSession(int index)
        {
            if (index < 0 || index >= sessions.Count) return;
            if (currentSessionIndex == index) return;

            currentSessionIndex = index;
            OnCurrentSessionChanged?.Invoke(currentSessionIndex);
        }

        public void NextSession()
        {
            if (sessions.Count <= 1) return;
            SelectSession((currentSessionIndex + 1) % sessions.Count);
        }

        public void PrevSession()
        {
            if (sessions.Count <= 1) return;
            SelectSession((currentSessionIndex - 1 + sessions.Count) % sessions.Count);
        }

        public void SetMaxConcurrentRepairs(int count)
        {
            maxConcurrentRepairs = Mathf.Clamp(count, 1, 3);
            Debug.Log($"[GarageManager] Постов ремонта: {maxConcurrentRepairs}");
        }

        public void EnableDoubleRepair() => SetMaxConcurrentRepairs(2);

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
            if (sessions.Count == 0) sb.AppendLine("  (пусто)");
            for (int i = 0; i < sessions.Count; i++)
            {
                var s = sessions[i];
                string mark = i == currentSessionIndex ? " ← активная" : "";
                sb.AppendLine($"  [{i}] {s.car.displayName} | state={s.state} | слот {s.slotIndex} | поломок: {s.brokenDownList.Count}{mark}");
            }
            Debug.Log(sb.ToString());
        }
    }

    public enum RepairState
    {
        NotDiagnosed,
        Diagnosed,
        Completed
    }

    [Serializable]
    public class RepairSession
    {
        public CarData car;
        public int slotIndex;
        public RepairState state;
        public List<BreakdownData> brokenDownList = new List<BreakdownData>();
        public List<BreakdownData> fixedList = new List<BreakdownData>();
        public List<BreakdownTimer> installingList = new List<BreakdownTimer>();

        [NonSerialized] public bool isCompleting; // защита от двойного клика
    }

    [Serializable]
    public class BreakdownTimer
    {
        public BreakdownData breakdown;
        public float startTime;
        public float endTime;
        public float totalDuration;

        public float TimeLeft => Mathf.Max(0f, endTime - Time.realtimeSinceStartup);

        public float Progress
        {
            get
            {
                if (totalDuration <= 0f) return 1f;
                return Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / totalDuration);
            }
        }
    }
}