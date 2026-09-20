using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Core;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Ремонт: ставит детали, начисляет рефанд, завершает заказ.
    /// Экономика: рефанд при установке + награда за заказ по редкости машины и поломок.
    /// </summary>
    public class RepairManager : MonoBehaviour
    {
        public static RepairManager Instance { get; private set; }

        [Header("Профит по редкости машины")]
        [Tooltip("Basic, Medium, Premium, Luxury, Secret — прибавка к окупаемости")]
        [SerializeField] private float[] machineProfitRates = { 0.25f, 0.40f, 0.75f, 1.00f, 1.50f };

        [Header("Бонус к профиту за редкость поломок")]
        [Tooltip("Common, Uncommon, Rare, Epic")]
        [SerializeField] private float[] breakdownRarityBonus = { 0f, 0.05f, 0.15f, 0.30f };

        [Tooltip("Во сколько раз больше за бонусную машину")]
        [SerializeField] private int bonusMultiplier = 2;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ==================== ПОЧИНКА ОДНОЙ ПОЛОМКИ ====================

        /// <summary>Установить деталь. Начисляет рефанд = стоимость деталей.</summary>
        public bool TryRepair(int sessionIndex, BreakdownData breakdown)
        {
            var reason = GetRepairFailReason(sessionIndex, breakdown);
            if (reason != null)
            {
                Debug.Log($"[RepairManager] Нельзя починить: {reason}");
                return false;
            }

            // Списываем детали
            foreach (var part in breakdown.requiredParts)
                InventoryManager.Instance.Remove(part.id, 1);

            // Рефанд — возвращаем стоимость деталей
            int refund = breakdown.GetPartsCost();
            if (EconomyManager.Instance != null && refund > 0)
            {
                EconomyManager.Instance.Add(refund);
                Debug.Log($"[RepairManager] Рефанд за «{breakdown.displayName}»: +${refund}");
            }

            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.MarkFixed(sessionIndex, breakdown);

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
                Debug.LogWarning("[RepairManager] Не все поломки устранены");
                return false;
            }

            var session = sessions[sessionIndex];
            int reward = CalculateOrderReward(session);

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Add(reward);

            string bonusTag = session.car.isBonus ? " (бонус ⭐ ×2)" : "";
            Debug.Log($"[RepairManager] Заказ завершён: {session.car.displayName}, +${reward}{bonusTag}");

            if (CollectionManager.Instance != null)
                CollectionManager.Instance.MarkRepaired(session.car);

            GarageManager.Instance.CompleteRepair(sessionIndex);
            return true;
        }

        /// <summary>
        /// Награда за заказ = стоимость всех деталей × профит-ставка.
        /// Профит-ставка = ставка машины + бонус за самую редкую поломку.
        /// </summary>
        private int CalculateOrderReward(RepairSession session)
        {
            if (session == null || session.car == null) return 0;

            // Стоимость всех деталей по всем поломкам
            int totalPartsCost = 0;
            PartRarity maxBreakdownRarity = PartRarity.Common;

            foreach (var bd in session.brokenDownList)
            {
                if (bd == null) continue;
                totalPartsCost += bd.GetPartsCost();
                var r = bd.GetRarity();
                if (r > maxBreakdownRarity) maxBreakdownRarity = r;
            }

            // Если деталей не было (не должно случиться) — минимальная награда
            if (totalPartsCost <= 0) return 50;

            // Ставка машины
            float machineRate = 0.25f;
            int mIdx = (int)session.car.rarity;
            if (machineProfitRates != null && mIdx >= 0 && mIdx < machineProfitRates.Length)
                machineRate = machineProfitRates[mIdx];

            // Бонус за редкость поломки
            float rarityBonus = 0f;
            int rIdx = (int)maxBreakdownRarity;
            if (breakdownRarityBonus != null && rIdx >= 0 && rIdx < breakdownRarityBonus.Length)
                rarityBonus = breakdownRarityBonus[rIdx];

            // Итоговая ставка
            float profitRate = machineRate + rarityBonus;
            int reward = Mathf.RoundToInt(totalPartsCost * profitRate);

            // Бонусная машина
            if (session.car.isBonus) reward *= bonusMultiplier;

            // Округляем до красивого числа
            if (reward >= 10000) reward = Mathf.RoundToInt(reward / 1000f) * 1000;
            else if (reward >= 1000) reward = Mathf.RoundToInt(reward / 100f) * 100;
            else if (reward >= 100) reward = Mathf.RoundToInt(reward / 10f) * 10;

            return reward;
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

        [ContextMenu("ТЕСТ: показать экономику сессии 0")]
        private void TestShowEconomics0()
        {
            if (GarageManager.Instance == null || GarageManager.Instance.Sessions.Count == 0)
            {
                Debug.Log("[RepairManager] Сессия 0 пуста");
                return;
            }

            var session = GarageManager.Instance.Sessions[0];
            int cost = 0;
            PartRarity maxR = PartRarity.Common;

            foreach (var bd in session.brokenDownList)
            {
                if (bd == null) continue;
                int bdCost = bd.GetPartsCost();
                cost += bdCost;
                var r = bd.GetRarity();
                if (r > maxR) maxR = r;
            }

            int mIdx = (int)session.car.rarity;
            float mRate = (machineProfitRates != null && mIdx < machineProfitRates.Length)
                ? machineProfitRates[mIdx] : 0.25f;
            int rIdx = (int)maxR;
            float rBonus = (breakdownRarityBonus != null && rIdx < breakdownRarityBonus.Length)
                ? breakdownRarityBonus[rIdx] : 0f;

            int reward = CalculateOrderReward(session);

            var sb = new System.Text.StringBuilder("[RepairManager] Экономика сессии 0:\n");
            sb.AppendLine($"  Машина: {session.car.displayName} [{session.car.rarity}]");
            sb.AppendLine($"  Детали (всего): ${cost}");
            sb.AppendLine($"  Макс. редкость поломки: {maxR}");
            sb.AppendLine($"  Ставка машины: +{mRate * 100:F0}%");
            sb.AppendLine($"  Бонус поломки: +{rBonus * 100:F0}%");
            sb.AppendLine($"  Итоговый профит: +${reward}");
            sb.AppendLine($"  Окупаемость: ×{(cost + reward) / (float)cost:F2}");
            Debug.Log(sb.ToString());
        }
    }
}