using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Core;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Диагностика: за деньги показывает список поломок у машины в гараже.
    /// Хранит статус каждой поломки (✅/❌) и следит за готовностью заказа.
    /// </summary>
    public class DiagnosticManager : MonoBehaviour
    {
        public static DiagnosticManager Instance { get; private set; }

        [Header("Настройки")]
        [Tooltip("Сколько стоит диагностика одной машины")]
        [SerializeField] private int diagnosisCost = 10;

        [Tooltip("Минимум поломок у машины после диагностики")]
        [SerializeField] private int minBreakdowns = 1;

        [Tooltip("Максимум поломок у машины после диагностики")]
        [SerializeField] private int maxBreakdowns = 3;

        public int DiagnosisCost => diagnosisCost;

        /// <summary>Аргумент — индекс сессии в GarageManager.Sessions.</summary>
        public event Action<int> OnDiagnosticsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ==================== ПУБЛИЧНОЕ API ====================

        /// <summary>
        /// Провести диагностику. Списывает деньги и генерирует поломки.
        /// Возвращает false, если сессии нет, уже диагностирована или денег мало.
        /// </summary>
        public bool RunDiagnosis(int sessionIndex)
        {
            if (GarageManager.Instance == null)
            {
                Debug.LogError("[DiagnosticManager] GarageManager не найден на сцене!");
                return false;
            }

            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count)
            {
                Debug.LogWarning($"[DiagnosticManager] Сессия {sessionIndex} не существует");
                return false;
            }

            var session = sessions[sessionIndex];

            if (session.state != RepairState.NotDiagnosed)
            {
                Debug.Log($"[DiagnosticManager] Сессия {sessionIndex} уже диагностирована");
                return false;
            }

            if (EconomyManager.Instance == null)
            {
                Debug.LogError("[DiagnosticManager] EconomyManager не найден!");
                return false;
            }

            // Пытаемся списать деньги.
            // ВАЖНО: если в твоём EconomyManager метод называется иначе — см. таблицу ошибок ниже.
            if (!SpendMoney(diagnosisCost))
            {
                Debug.Log($"[DiagnosticManager] Не хватает денег на диагностику (${diagnosisCost})");
                return false;
            }

            // Собираем пул поломок
            var pool = new List<BreakdownData>();
            if (session.car.possibleBreakdowns != null)
            {
                foreach (var bd in session.car.possibleBreakdowns)
                    if (bd != null) pool.Add(bd);
            }

            session.brokenDownList.Clear();
            session.fixedList.Clear();

            if (pool.Count == 0)
            {
                Debug.LogWarning($"[DiagnosticManager] У машины {session.car.id} нет поломок — сразу готово");
                session.state = RepairState.Diagnosed;
                OnDiagnosticsUpdated?.Invoke(sessionIndex);
                return true;
            }

            // Сколько поломок выпадет
            int wanted = UnityEngine.Random.Range(minBreakdowns, maxBreakdowns + 1);
            int count = Mathf.Clamp(wanted, 1, pool.Count);

            Shuffle(pool);
            for (int i = 0; i < count; i++)
                session.brokenDownList.Add(pool[i]);

            session.state = RepairState.Diagnosed;

            Debug.Log($"[DiagnosticManager] Сессия {sessionIndex}: диагностика за ${diagnosisCost}, найдено поломок: {count}");
            foreach (var bd in session.brokenDownList)
                Debug.Log($"   → {bd.displayName}");

            OnDiagnosticsUpdated?.Invoke(sessionIndex);
            return true;
        }

        /// <summary>Список поломок сессии (после диагностики).</summary>
        public IReadOnlyList<BreakdownData> GetBreakdowns(int sessionIndex)
        {
            if (GarageManager.Instance == null) return Array.Empty<BreakdownData>();
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return Array.Empty<BreakdownData>();
            return sessions[sessionIndex].brokenDownList;
        }

        /// <summary>Устранена ли конкретная поломка в сессии.</summary>
        public bool IsFixed(int sessionIndex, BreakdownData breakdown)
        {
            if (GarageManager.Instance == null || breakdown == null) return false;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return false;
            return sessions[sessionIndex].fixedList.Contains(breakdown);
        }

        /// <summary>Пометить поломку как устранённую. Вызывает RepairManager.</summary>
        public void MarkFixed(int sessionIndex, BreakdownData breakdown)
        {
            if (GarageManager.Instance == null || breakdown == null) return;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return;

            var session = sessions[sessionIndex];
            if (!session.brokenDownList.Contains(breakdown)) return;
            if (session.fixedList.Contains(breakdown)) return;

            session.fixedList.Add(breakdown);
            Debug.Log($"[DiagnosticManager] Починено: {breakdown.displayName}");

            if (IsSessionComplete(sessionIndex))
            {
                session.state = RepairState.Completed;
                Debug.Log($"[DiagnosticManager] Сессия {sessionIndex} готова к завершению!");
            }

            OnDiagnosticsUpdated?.Invoke(sessionIndex);
        }

        /// <summary>Все ли поломки сессии устранены (машина готова к сдаче).</summary>
        public bool IsSessionComplete(int sessionIndex)
        {
            if (GarageManager.Instance == null) return false;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return false;

            var session = sessions[sessionIndex];
            if (session.state == RepairState.NotDiagnosed) return false;
            if (session.brokenDownList.Count == 0) return true;

            foreach (var bd in session.brokenDownList)
                if (!session.fixedList.Contains(bd)) return false;

            return true;
        }

        // ==================== УТИЛИТЫ ====================

        /// <summary>
        /// Обёртка над EconomyManager, чтобы не зависеть от точного имени метода.
        /// Пробует самые частые варианты: TrySpend / Spend / Remove / Withdraw.
        /// </summary>
        private bool SpendMoney(int amount)
        {
            var eco = EconomyManager.Instance;
            if (eco == null) return false;

            // Если у тебя в EconomyManager есть готовый метод — допиши его первым в список.
            // Например: if (eco.TrySpend(amount)) return true;

            // А пока — работаем через публичное API которое точно есть у тебя:
            // у тебя в InventoryManager вызывается eco.Add(...) — значит метод Add есть.
            // А вот метода "потратить" мы не видели. Поэтому используем рефлексию-фолбэк.

            // 1) Прямой вариант через TrySpend
            var m1 = eco.GetType().GetMethod("TrySpend", new[] { typeof(int) });
            if (m1 != null && m1.ReturnType == typeof(bool))
                return (bool)m1.Invoke(eco, new object[] { amount });

            // 2) Spend
            var m2 = eco.GetType().GetMethod("Spend", new[] { typeof(int) });
            if (m2 != null && m2.ReturnType == typeof(bool))
                return (bool)m2.Invoke(eco, new object[] { amount });

            // 3) Remove
            var m3 = eco.GetType().GetMethod("Remove", new[] { typeof(int) });
            if (m3 != null && m3.ReturnType == typeof(bool))
                return (bool)m3.Invoke(eco, new object[] { amount });

            // 4) Withdraw
            var m4 = eco.GetType().GetMethod("Withdraw", new[] { typeof(int) });
            if (m4 != null && m4.ReturnType == typeof(bool))
                return (bool)m4.Invoke(eco, new object[] { amount });

            // 5) Ручной фолбэк: проверить баланс и вычесть через Add(-amount)
            var propMoney = eco.GetType().GetProperty("Money");
            var fieldMoney = eco.GetType().GetField("money", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            int current = 0;
            if (propMoney != null && propMoney.PropertyType == typeof(int))
                current = (int)propMoney.GetValue(eco);
            else if (fieldMoney != null)
                current = (int)fieldMoney.GetValue(eco);

            if (current < amount) return false;

            var add = eco.GetType().GetMethod("Add", new[] { typeof(int) });
            if (add != null)
            {
                add.Invoke(eco, new object[] { -amount });
                return true;
            }

            Debug.LogError("[DiagnosticManager] Не нашёл способ списать деньги в EconomyManager. " +
                           "Открой EconomyManager.cs и скажи, как называется метод траты — я поправлю.");
            return false;
        }

        /// <summary>Перемешивание Фишера-Йетса.</summary>
        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ==================== ТЕСТЫ (ПКМ по компоненту) ====================

        [ContextMenu("ТЕСТ: диагностировать сессию 0")]
        private void TestDiagnose0() => RunDiagnosis(0);

        [ContextMenu("ТЕСТ: показать статус сессии 0")]
        private void TestDump0()
        {
            var list = GetBreakdowns(0);
            var sb = new System.Text.StringBuilder($"[DiagnosticManager] Сессия 0, поломок: {list.Count}\n");
            foreach (var bd in list)
                sb.AppendLine($"  {(IsFixed(0, bd) ? "✅" : "❌")} {bd.displayName}");
            sb.AppendLine($"  Complete: {IsSessionComplete(0)}");
            Debug.Log(sb.ToString());
        }

        [ContextMenu("ТЕСТ: починить всё в сессии 0")]
        private void TestFixAll0()
        {
            var list = GetBreakdowns(0);
            for (int i = list.Count - 1; i >= 0; i--)
                MarkFixed(0, list[i]);
        }
    }
}