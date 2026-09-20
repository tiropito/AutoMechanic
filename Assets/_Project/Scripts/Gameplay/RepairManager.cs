using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Core;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Ремонт: ставит детали из инвентаря, начисляет награду за поломку, завершает заказ.
    /// </summary>
    public class RepairManager : MonoBehaviour
    {
        public static RepairManager Instance { get; private set; }

        [Header("Награда за завершение заказа")]
        [SerializeField] private int minOrderReward = 50;
        [SerializeField] private int maxOrderReward = 200;

        [Tooltip("Во сколько раз больше платят за бонусную машину (⭐)")]
        [SerializeField] private int bonusMultiplier = 2;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ==================== ПОЧИНКА ОДНОЙ ПОЛОМКИ ====================

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

            foreach (var part in breakdown.requiredParts)
            {
                bool ok = InventoryManager.Instance.Remove(part.id, 1);
                if (!ok)
                {
                    Debug.LogError($"[RepairManager] Гонка при списании {part.id}. Инвентарь мог измениться.");
                    return false;
                }
            }

            if (EconomyManager.Instance != null && breakdown.repairReward > 0)
            {
                EconomyManager.Instance.Add(breakdown.repairReward);
                Debug.Log($"[RepairManager] +${breakdown.repairReward} за «{breakdown.displayName}»");
            }

            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.MarkFixed(sessionIndex, breakdown);

            Debug.Log($"[RepairManager] Поломка «{breakdown.displayName}» устранена");
            return true;
        }

        public bool CanRepair(int sessionIndex, BreakdownData breakdown, out string failReason)
        {
            failReason = GetRepairFailReason(sessionIndex, breakdown);
            return failReason == null;
        }

        private string GetRepairFailReason(int sessionIndex, BreakdownData breakdown)
        {
            if (GarageManager.Instance == null) return "GarageManager не найден";
            if (DiagnosticManager.Instance == null) return "DiagnosticManager не найден";
            if (InventoryManager.Instance == null) return "InventoryManager не найден";
            if (breakdown == null) return "Поломка не указана";

            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count)
                return $"Сессия {sessionIndex} не существует";

            var session = sessions[sessionIndex];

            if (session.state == RepairState.NotDiagnosed) return "Сначала нужно провести диагностику";
            if (session.state == RepairState.Completed) return "Машина уже полностью починена";
            if (!session.brokenDownList.Contains(breakdown))
                return $"Поломка «{breakdown.displayName}» не относится к этой машине";
            if (session.fixedList.Contains(breakdown))
                return $"Поломка «{breakdown.displayName}» уже устранена";
            if (breakdown.requiredParts == null || breakdown.requiredParts.Length == 0)
                return $"У поломки «{breakdown.displayName}» не заданы детали";

            foreach (var part in breakdown.requiredParts)
            {
                if (part == null) return $"Пустая ссылка на деталь в «{breakdown.displayName}»";
                int have = InventoryManager.Instance.GetCount(part.id);
                if (have < 1) return $"Не хватает детали «{part.displayName}» (есть {have})";
            }

            return null;
        }

        // ==================== ЗАВЕРШЕНИЕ ЗАКАЗА ====================

        /// <summary>Сдать машину заказчику. Работает только если все поломки устранены.</summary>
        public bool TryCompleteOrder(int sessionIndex)
        {
            if (GarageManager.Instance == null || DiagnosticManager.Instance == null)
            {
                Debug.LogError("[RepairManager] Нет GarageManager или DiagnosticManager");
                return false;
            }

            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count)
            {
                Debug.LogWarning($"[RepairManager] Сессия {sessionIndex} не существует");
                return false;
            }

            if (!DiagnosticManager.Instance.IsSessionComplete(sessionIndex))
            {
                Debug.LogWarning("[RepairManager] Не все поломки устранены — заказ не готов");
                return false;
            }

            var session = sessions[sessionIndex];
            int reward = UnityEngine.Random.Range(minOrderReward, maxOrderReward + 1);
            if (session.car.isBonus) reward *= bonusMultiplier;

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Add(reward);

            string bonusTag = session.car.isBonus ? " (бонус ⭐ ×2)" : "";
            Debug.Log($"[RepairManager] Заказ завершён: {session.car.displayName}, +${reward}{bonusTag}");
            
            // Отмечаем в коллекции
            if (CollectionManager.Instance != null)
                CollectionManager.Instance.MarkRepaired(session.car);

            GarageManager.Instance.CompleteRepair(sessionIndex);
            return true;
        }

        // ==================== ТЕСТЫ ====================

        [ContextMenu("ТЕСТ: починить ВСЁ в сессии 0")]
        private void TestRepairAll0()
        {
            var list = DiagnosticManager.Instance != null
                ? DiagnosticManager.Instance.GetBreakdowns(0) : null;
            if (list == null || list.Count == 0) { Debug.Log("[RepairManager] Сессия 0 пуста"); return; }
            for (int i = list.Count - 1; i >= 0; i--) TryRepair(0, list[i]);
        }

        [ContextMenu("ТЕСТ: завершить заказ 0")]
        private void TestComplete0() => TryCompleteOrder(0);

        [ContextMenu("ТЕСТ: показать требования по сессии 0")]
        private void TestShowRequirements0()
        {
            var list = DiagnosticManager.Instance != null
                ? DiagnosticManager.Instance.GetBreakdowns(0) : null;
            if (list == null || list.Count == 0) { Debug.Log("[RepairManager] Сессия 0 пуста"); return; }

            var sb = new System.Text.StringBuilder("[RepairManager] Требования по сессии 0:\n");
            foreach (var bd in list)
            {
                bool fixedAlready = DiagnosticManager.Instance.IsFixed(0, bd);
                bool can = CanRepair(0, bd, out string reason);
                sb.Append($"  {bd.displayName} | ${bd.repairReward} | ");
                sb.AppendLine(fixedAlready ? "уже ✅" : (can ? "можно ✅" : $"нельзя ❌ ({reason})"));
                if (bd.requiredParts != null)
                    foreach (var p in bd.requiredParts)
                    {
                        int have = InventoryManager.Instance != null ? InventoryManager.Instance.GetCount(p.id) : 0;
                        sb.AppendLine($"      • {p.displayName} (есть {have})");
                    }
            }
            Debug.Log(sb.ToString());
        }
    }
}