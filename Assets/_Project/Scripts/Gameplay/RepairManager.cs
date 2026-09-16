using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Core;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Ремонт: ставит детали из инвентаря на машину.
    /// Проверяет наличие ВСЕХ деталей, нужных для поломки (у ржавчины их 2).
    /// Выдаёт деньги за починку и помечает поломку как устранённую.
    /// </summary>
    public class RepairManager : MonoBehaviour
    {
        public static RepairManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ==================== ПУБЛИЧНОЕ API ====================

        /// <summary>
        /// Попытаться починить поломку в сессии.
        /// Возвращает true только если ВСЕ нужные детали были в наличии и списаны.
        /// </summary>
        public bool TryRepair(int sessionIndex, BreakdownData breakdown)
        {
            var reason = GetRepairFailReason(sessionIndex, breakdown);
            if (reason != null)
            {
                Debug.Log($"[RepairManager] Нельзя починить: {reason}");
                return false;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[RepairManager] InventoryManager не найден!");
                return false;
            }

            // Списываем все нужные детали
            foreach (var part in breakdown.requiredParts)
            {
                bool ok = InventoryManager.Instance.Remove(part.id, 1);
                if (!ok)
                {
                    // Сюда попасть не должны — проверка уже прошла в GetRepairFailReason,
                    // но на всякий случай откатываем то, что успели списать.
                    Debug.LogError($"[RepairManager] Гонка при списании {part.id}. Инвентарь мог измениться.");
                    return false;
                }
            }

            // Выдаём деньги
            if (EconomyManager.Instance != null && breakdown.repairReward > 0)
            {
                EconomyManager.Instance.Add(breakdown.repairReward);
                Debug.Log($"[RepairManager] +${breakdown.repairReward} за устранение «{breakdown.displayName}»");
            }

            // Помечаем поломку как устранённую
            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.MarkFixed(sessionIndex, breakdown);

            Debug.Log($"[RepairManager] Поломка «{breakdown.displayName}» устранена");
            return true;
        }

        /// <summary>
        /// Удобный метод для UI: можно ли починить прямо сейчас.
        /// Возвращает true, если можно. Если нельзя — в failReason положит причину.
        /// </summary>
        public bool CanRepair(int sessionIndex, BreakdownData breakdown, out string failReason)
        {
            failReason = GetRepairFailReason(sessionIndex, breakdown);
            return failReason == null;
        }

        /// <summary>
        /// Проверка всех условий. null = можно чинить, иначе — текст причины.
        /// </summary>
        private string GetRepairFailReason(int sessionIndex, BreakdownData breakdown)
        {
            if (GarageManager.Instance == null)
                return "GarageManager не найден на сцене";

            if (DiagnosticManager.Instance == null)
                return "DiagnosticManager не найден на сцене";

            if (InventoryManager.Instance == null)
                return "InventoryManager не найден на сцене";

            if (breakdown == null)
                return "Поломка не указана";

            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count)
                return $"Сессия {sessionIndex} не существует";

            var session = sessions[sessionIndex];

            if (session.state == RepairState.NotDiagnosed)
                return "Сначала нужно провести диагностику";

            if (session.state == RepairState.Completed)
                return "Машина уже полностью починена";

            if (!session.brokenDownList.Contains(breakdown))
                return $"Поломка «{breakdown.displayName}» не относится к этой машине";

            if (session.fixedList.Contains(breakdown))
                return $"Поломка «{breakdown.displayName}» уже устранена";

            if (breakdown.requiredParts == null || breakdown.requiredParts.Length == 0)
                return $"У поломки «{breakdown.displayName}» не заданы детали в ScriptableObject";

            // Проверяем, что ВСЕ нужные детали есть в наличии
            foreach (var part in breakdown.requiredParts)
            {
                if (part == null)
                    return $"В поломке «{breakdown.displayName}» пустая ссылка на деталь";

                int have = InventoryManager.Instance.GetCount(part.id);
                if (have < 1)
                    return $"Не хватает детали «{part.displayName}» (нужно 1, есть {have})";
            }

            return null; // всё ок
        }

        // ==================== ТЕСТЫ (ПКМ по компоненту) ====================

        [ContextMenu("ТЕСТ: починить первую непочиненную поломку в сессии 0")]
        private void TestRepairFirst0()
        {
            var list = DiagnosticManager.Instance != null
                ? DiagnosticManager.Instance.GetBreakdowns(0)
                : null;

            if (list == null || list.Count == 0)
            {
                Debug.Log("[RepairManager] Сессия 0 пуста или не диагностирована");
                return;
            }

            foreach (var bd in list)
            {
                if (DiagnosticManager.Instance.IsFixed(0, bd)) continue;
                bool ok = TryRepair(0, bd);
                Debug.Log(ok ? $"✅ Починили {bd.displayName}" : $"❌ Не смогли починить {bd.displayName}");
                return;
            }
            Debug.Log("[RepairManager] Все поломки в сессии 0 уже устранены");
        }

        [ContextMenu("ТЕСТ: починить ВСЁ в сессии 0")]
        private void TestRepairAll0()
        {
            var list = DiagnosticManager.Instance != null
                ? DiagnosticManager.Instance.GetBreakdowns(0)
                : null;

            if (list == null || list.Count == 0)
            {
                Debug.Log("[RepairManager] Сессия 0 пуста или не диагностирована");
                return;
            }

            // идём с конца, т.к. MarkFixed меняет fixedList
            for (int i = list.Count - 1; i >= 0; i--)
            {
                TryRepair(0, list[i]);
            }
        }

        [ContextMenu("ТЕСТ: показать требования по сессии 0")]
        private void TestShowRequirements0()
        {
            var list = DiagnosticManager.Instance != null
                ? DiagnosticManager.Instance.GetBreakdowns(0)
                : null;

            if (list == null || list.Count == 0)
            {
                Debug.Log("[RepairManager] Сессия 0 пуста или не диагностирована");
                return;
            }

            var sb = new System.Text.StringBuilder("[RepairManager] Требования по сессии 0:\n");
            foreach (var bd in list)
            {
                bool fixedAlready = DiagnosticManager.Instance.IsFixed(0, bd);
                bool can = CanRepair(0, bd, out string reason);

                sb.Append($"  {bd.displayName} | reward=${bd.repairReward} | ");
                sb.Append(fixedAlready ? "уже ✅" : (can ? "можно чинить ✅" : $"нельзя ❌ ({reason})"));
                sb.AppendLine();

                if (bd.requiredParts != null)
                {
                    foreach (var p in bd.requiredParts)
                    {
                        int have = InventoryManager.Instance != null ? InventoryManager.Instance.GetCount(p.id) : 0;
                        sb.AppendLine($"      • {p.displayName} (нужно 1, есть {have})");
                    }
                }
            }
            Debug.Log(sb.ToString());
        }
    }
}