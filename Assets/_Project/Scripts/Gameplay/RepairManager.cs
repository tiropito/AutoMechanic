using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Core;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Ремонт: ставит детали с таймером, завершает заказ, продаёт недоремонтированную машину.
    /// Защита от двойного клика через _lockedSessions.
    /// </summary>
    public class RepairManager : MonoBehaviour
    {
        public static RepairManager Instance { get; private set; }

        [Header("Время установки (по редкости поломки)")]
        [Tooltip("Common, Uncommon, Rare, Epic — секунды")]
        [SerializeField] private float[] installTimes = { 3f, 5f, 10f, 20f };

        [Header("Профит по редкости машины")]
        [Tooltip("Basic, Medium, Premium, Luxury, Secret")]
        [SerializeField] private float[] machineProfitRates = { 0.60f, 0.85f, 1.20f, 1.60f, 2.20f };

        [Header("Бонус к профиту за редкость поломок")]
        [Tooltip("Common, Uncommon, Rare, Epic")]
        [SerializeField] private float[] breakdownRarityBonus = { 0f, 0.05f, 0.15f, 0.30f };

        [Tooltip("Во сколько раз больше за бонусную машину")]
        [SerializeField] private int bonusMultiplier = 2;

        [Header("Продажа недоремонтированной машины")]
        [SerializeField] private float sellAsIsRate = 0.4f;
        [SerializeField] private int sellAsIsNoRepairFee = 50;

        [Header("Завершение заказа")]
        [Tooltip("Минимальная сумма за завершённый заказ")]
        [SerializeField] private int minOrderReward = 50;

        private readonly HashSet<int> _lockedSessions = new HashSet<int>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            TickTimers();
        }

        // ==================== ТАЙМЕРЫ ====================

        private void TickTimers()
        {
            if (GarageManager.Instance == null) return;
            var sessions = GarageManager.Instance.Sessions;

            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                if (session.installingList == null || session.installingList.Count == 0) continue;

                for (int j = session.installingList.Count - 1; j >= 0; j--)
                {
                    var timer = session.installingList[j];
                    if (timer == null) { session.installingList.RemoveAt(j); continue; }

                    if (Time.realtimeSinceStartup >= timer.endTime)
                    {
                        session.installingList.RemoveAt(j);
                        CompleteInstall(i, timer.breakdown);
                    }
                }
            }
        }

        private void CompleteInstall(int sessionIndex, BreakdownData breakdown)
        {
            Debug.Log($"[RepairManager] Установка «{breakdown.displayName}» завершена");

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayFix();

            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.MarkFixed(sessionIndex, breakdown);
        }

        // ==================== ПОЧИНКА ====================

        public bool TryRepair(int sessionIndex, BreakdownData breakdown)
        {
            var reason = GetRepairFailReason(sessionIndex, breakdown);
            if (reason != null)
            {
                Debug.Log($"[RepairManager] Нельзя починить: {reason}");
                return false;
            }

            foreach (var part in breakdown.requiredParts)
                InventoryManager.Instance.Remove(part.id, 1);

            var session = GarageManager.Instance.Sessions[sessionIndex];
            if (session.installingList == null) session.installingList = new List<BreakdownTimer>();

            float duration = GetInstallTime(breakdown);
            float now = Time.realtimeSinceStartup;

            session.installingList.Add(new BreakdownTimer
            {
                breakdown = breakdown,
                startTime = now,
                endTime = now + duration,
                totalDuration = duration
            });

            Debug.Log($"[RepairManager] Установка «{breakdown.displayName}»: {duration} сек");

            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.NotifyChanged(sessionIndex);

            return true;
        }

        public bool CanRepair(int sessionIndex, BreakdownData breakdown, out string failReason)
        {
            failReason = GetRepairFailReason(sessionIndex, breakdown);
            return failReason == null;
        }

        public bool IsInstalling(int sessionIndex, BreakdownData breakdown)
        {
            if (GarageManager.Instance == null || breakdown == null) return false;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return false;

            var session = sessions[sessionIndex];
            if (session.installingList == null) return false;

            foreach (var t in session.installingList)
                if (t != null && t.breakdown == breakdown) return true;
            return false;
        }

        public float GetInstallTimeLeft(int sessionIndex, BreakdownData breakdown)
        {
            if (GarageManager.Instance == null || breakdown == null) return 0f;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return 0f;

            var session = sessions[sessionIndex];
            if (session.installingList == null) return 0f;

            foreach (var t in session.installingList)
                if (t != null && t.breakdown == breakdown) return t.TimeLeft;
            return 0f;
        }

        public float GetInstallProgress(int sessionIndex, BreakdownData breakdown)
        {
            if (GarageManager.Instance == null || breakdown == null) return 0f;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return 0f;

            var session = sessions[sessionIndex];
            if (session.installingList == null) return 0f;

            foreach (var t in session.installingList)
                if (t != null && t.breakdown == breakdown) return t.Progress;
            return 0f;
        }

        private float GetInstallTime(BreakdownData breakdown)
        {
            if (breakdown == null) return 3f;
            int idx = (int)breakdown.GetRarity();
            if (installTimes != null && idx >= 0 && idx < installTimes.Length)
                return installTimes[idx];
            return 3f;
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

            if (IsInstalling(sessionIndex, breakdown))
                return $"«{breakdown.displayName}» уже устанавливается";

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

            if (_lockedSessions.Contains(sessionIndex))
            {
                Debug.Log("[RepairManager] Сессия уже завершается");
                return false;
            }

            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count)
            {
                Debug.LogWarning($"[RepairManager] Сессия {sessionIndex} не существует");
                return false;
            }

            var session = sessions[sessionIndex];

            if (session.installingList != null && session.installingList.Count > 0)
            {
                Debug.LogWarning("[RepairManager] Есть незавершённые установки");
                return false;
            }

            if (!DiagnosticManager.Instance.IsSessionComplete(sessionIndex))
            {
                Debug.LogWarning("[RepairManager] Не все поломки устранены");
                return false;
            }

            _lockedSessions.Add(sessionIndex);

            int reward = CalculateOrderReward(session);

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Add(reward);

            if (AutoMechanic.UI.MoneyFlyUI.Instance != null)
                AutoMechanic.UI.MoneyFlyUI.Instance.ShowReward(reward);

            if (AutoMechanic.UI.ConfettiUI.Instance != null)
                AutoMechanic.UI.ConfettiUI.Instance.Play(Vector2.zero);

            // Звук кассы
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCash();

            string bonusTag = session.car.isBonus ? " (бонус ⭐ ×2)" : "";
            Debug.Log($"[RepairManager] Заказ завершён: {session.car.displayName}, +${reward}{bonusTag}");

            if (CollectionManager.Instance != null)
                CollectionManager.Instance.MarkRepaired(session.car);

            var view = FindObjectOfType<AutoMechanic.UI.GarageView>();
            if (view != null)
            {
                view.PlayCarLeaveAnimation(() =>
                {
                    GarageManager.Instance.CompleteRepair(sessionIndex);
                    _lockedSessions.Remove(sessionIndex);
                });
            }
            else
            {
                GarageManager.Instance.CompleteRepair(sessionIndex);
                _lockedSessions.Remove(sessionIndex);
            }

            return true;
        }

        // ==================== ПРОДАЖА КАК ЕСТЬ ====================

        public bool TrySellAsIs(int sessionIndex)
        {
            if (GarageManager.Instance == null || EconomyManager.Instance == null) return false;

            if (_lockedSessions.Contains(sessionIndex))
            {
                Debug.Log("[RepairManager] Сессия уже завершается");
                return false;
            }

            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return false;

            var session = sessions[sessionIndex];
            int price = CalculateSellAsIsPrice(sessionIndex);
            int actualAmount = 0;

            if (price > 0)
            {
                EconomyManager.Instance.Add(price);
                actualAmount = price;
                Debug.Log($"[RepairManager] Продано как есть: {session.car.displayName}, +${price}");
            }
            else if (price < 0)
            {
                int fee = Mathf.Min(-price, EconomyManager.Instance.Money);
                if (fee > 0)
                {
                    EconomyManager.Instance.Spend(fee);
                    actualAmount = -fee;
                }
                Debug.Log($"[RepairManager] Отказ от заказа: {session.car.displayName}, −${fee} (штраф)");
            }
            else
            {
                Debug.Log($"[RepairManager] Продано как есть: {session.car.displayName}, +$0");
            }

            if (actualAmount != 0 && AutoMechanic.UI.MoneyFlyUI.Instance != null)
                AutoMechanic.UI.MoneyFlyUI.Instance.ShowAmount(actualAmount);

            // Звук — только при положительной сумме
            if (actualAmount > 0 && AudioManager.Instance != null)
                AudioManager.Instance.PlayCash();

            _lockedSessions.Add(sessionIndex);

            var view = FindObjectOfType<AutoMechanic.UI.GarageView>();
            if (view != null)
            {
                view.PlayCarLeaveAnimation(() =>
                {
                    GarageManager.Instance.CompleteRepair(sessionIndex);
                    _lockedSessions.Remove(sessionIndex);
                });
            }
            else
            {
                GarageManager.Instance.CompleteRepair(sessionIndex);
                _lockedSessions.Remove(sessionIndex);
            }

            return true;
        }

        public int CalculateSellAsIsPrice(int sessionIndex)
        {
            if (GarageManager.Instance == null) return 0;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return 0;

            var session = sessions[sessionIndex];

            int fixedCount = session.fixedList != null ? session.fixedList.Count : 0;
            if (fixedCount == 0)
                return -sellAsIsNoRepairFee;

            int partsCost = 0;
            if (session.fixedList != null)
                foreach (var bd in session.fixedList)
                    if (bd != null) partsCost += bd.GetPartsCost();

            int price = Mathf.RoundToInt(partsCost * sellAsIsRate);

            if (DiagnosticManager.Instance != null && session.car != null)
                price += DiagnosticManager.Instance.GetDiagnosisCost(session.car);

            if (price >= 1000) price = Mathf.RoundToInt(price / 100f) * 100;
            else if (price >= 100) price = Mathf.RoundToInt(price / 10f) * 10;
            else if (price >= 10) price = Mathf.RoundToInt(price / 5f) * 5;

            return price;
        }

        // ==================== ЭКОНОМИКА ЗАКАЗА ====================

        private int CalculateOrderReward(RepairSession session)
        {
            if (session == null || session.car == null) return minOrderReward;

            int totalPartsCost = 0;
            PartRarity maxBreakdownRarity = PartRarity.Common;

            foreach (var bd in session.brokenDownList)
            {
                if (bd == null) continue;
                totalPartsCost += bd.GetPartsCost();
                var r = bd.GetRarity();
                if (r > maxBreakdownRarity) maxBreakdownRarity = r;
            }

            float machineRate = 0.25f;
            int mIdx = (int)session.car.rarity;
            if (machineProfitRates != null && mIdx >= 0 && mIdx < machineProfitRates.Length)
                machineRate = machineProfitRates[mIdx];

            float rarityBonus = 0f;
            int rIdx = (int)maxBreakdownRarity;
            if (breakdownRarityBonus != null && rIdx >= 0 && rIdx < breakdownRarityBonus.Length)
                rarityBonus = breakdownRarityBonus[rIdx];

            float profitRate = 1f + machineRate + rarityBonus;
            int reward = Mathf.RoundToInt(totalPartsCost * profitRate);

            int diagCost = 0;
            if (DiagnosticManager.Instance != null)
                diagCost = DiagnosticManager.Instance.GetDiagnosisCost(session.car);
            reward += diagCost;

            reward = Mathf.Max(reward, minOrderReward);

            if (session.car.isBonus) reward *= bonusMultiplier;

            if (reward >= 10000) reward = Mathf.RoundToInt(reward / 1000f) * 1000;
            else if (reward >= 1000) reward = Mathf.RoundToInt(reward / 100f) * 100;
            else if (reward >= 100) reward = Mathf.RoundToInt(reward / 10f) * 10;

            return reward;
        }

        // ==================== ТЕСТЫ ====================

        [ContextMenu("ТЕСТ: завершить все таймеры сессии 0")]
        private void TestFinishTimers0()
        {
            if (GarageManager.Instance == null || GarageManager.Instance.Sessions.Count == 0) return;

            var session = GarageManager.Instance.Sessions[0];
            if (session.installingList == null || session.installingList.Count == 0) return;

            for (int i = session.installingList.Count - 1; i >= 0; i--)
            {
                var timer = session.installingList[i];
                if (timer == null) continue;
                session.installingList.RemoveAt(i);
                CompleteInstall(0, timer.breakdown);
            }
        }

        [ContextMenu("ТЕСТ: завершить заказ 0")]
        private void TestComplete0() => TryCompleteOrder(0);

        [ContextMenu("ТЕСТ: продать как есть сессию 0")]
        private void TestSellAsIs0() => TrySellAsIs(0);
    }
}