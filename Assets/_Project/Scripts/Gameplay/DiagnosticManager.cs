using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Core;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    public class DiagnosticManager : MonoBehaviour
    {
        public static DiagnosticManager Instance { get; private set; }

        [Header("Стоимость диагностики по редкости машины")]
        [Tooltip("Basic, Medium, Premium, Luxury, Secret")]
        [SerializeField] private int[] diagnosisCosts = { 10, 25, 50, 100, 200 };

        [Header("Количество поломок")]
        [SerializeField] private int minBreakdowns = 1;
        [SerializeField] private int maxBreakdowns = 3;

        [Header("Веса редкости (шанс выпадения)")]
        [Header("Веса редкости (шанс выпадения)")]
        [SerializeField] private float weightCommon = 100f;
        [SerializeField] private float weightUncommon = 60f;
        [SerializeField] private float weightRare = 15f;
        [SerializeField] private float weightEpic = 1f;

        public event Action<int> OnDiagnosticsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ==================== СТОИМОСТЬ ====================

        public int GetDiagnosisCost(CarData car)
        {
            if (car == null) return 10;
            int idx = (int)car.rarity;
            if (diagnosisCosts != null && idx >= 0 && idx < diagnosisCosts.Length)
                return diagnosisCosts[idx];
            return 10;
        }

        public int GetDiagnosisCost(int sessionIndex)
        {
            if (GarageManager.Instance == null) return 10;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return 10;
            return GetDiagnosisCost(sessions[sessionIndex].car);
        }

        // ==================== ДИАГНОСТИКА ====================

        public bool RunDiagnosis(int sessionIndex)
        {
            if (GarageManager.Instance == null)
            {
                Debug.LogError("[DiagnosticManager] GarageManager не найден!");
                return false;
            }

            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return false;

            var session = sessions[sessionIndex];
            if (session.state != RepairState.NotDiagnosed) return false;

            if (EconomyManager.Instance == null) return false;

            int cost = GetDiagnosisCost(session.car);
            if (!EconomyManager.Instance.Spend(cost))
            {
                Debug.Log($"[DiagnosticManager] Не хватает на диагностику (${cost})");
                return false;
            }

            var pool = new List<BreakdownData>();
            if (session.car.possibleBreakdowns != null)
                foreach (var bd in session.car.possibleBreakdowns)
                    if (bd != null) pool.Add(bd);

            session.brokenDownList.Clear();
            session.fixedList.Clear();

            if (pool.Count == 0)
            {
                session.state = RepairState.Diagnosed;
                OnDiagnosticsUpdated?.Invoke(sessionIndex);
                return true;
            }

            int wanted = UnityEngine.Random.Range(minBreakdowns, maxBreakdowns + 1);
            int count = Mathf.Clamp(wanted, 1, pool.Count);

            var available = new List<BreakdownData>(pool);
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                var picked = PickWeighted(available);
                session.brokenDownList.Add(picked);
                available.Remove(picked);
            }

            session.state = RepairState.Diagnosed;

            Debug.Log($"[DiagnosticManager] Сессия {sessionIndex}: диагностика за ${cost}, поломок: {count}");
            foreach (var bd in session.brokenDownList)
                Debug.Log($"   → {bd.displayName} (вес: {GetBreakdownWeight(bd)})");

            OnDiagnosticsUpdated?.Invoke(sessionIndex);
            return true;
        }

        public IReadOnlyList<BreakdownData> GetBreakdowns(int sessionIndex)
        {
            if (GarageManager.Instance == null) return Array.Empty<BreakdownData>();
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return Array.Empty<BreakdownData>();
            return sessions[sessionIndex].brokenDownList;
        }

        public bool IsFixed(int sessionIndex, BreakdownData breakdown)
        {
            if (GarageManager.Instance == null || breakdown == null) return false;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return false;
            return sessions[sessionIndex].fixedList.Contains(breakdown);
        }

        public void MarkFixed(int sessionIndex, BreakdownData breakdown)
        {
            if (GarageManager.Instance == null || breakdown == null) return;
            var sessions = GarageManager.Instance.Sessions;
            if (sessionIndex < 0 || sessionIndex >= sessions.Count) return;

            var session = sessions[sessionIndex];
            if (!session.brokenDownList.Contains(breakdown)) return;
            if (session.fixedList.Contains(breakdown)) return;

            session.fixedList.Add(breakdown);

            if (IsSessionComplete(sessionIndex))
            {
                session.state = RepairState.Completed;
                Debug.Log($"[DiagnosticManager] Сессия {sessionIndex} готова!");
            }

            OnDiagnosticsUpdated?.Invoke(sessionIndex);
        }

        public void NotifyChanged(int sessionIndex)
        {
            OnDiagnosticsUpdated?.Invoke(sessionIndex);
        }

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

        private BreakdownData PickWeighted(List<BreakdownData> pool)
        {
            float totalWeight = 0f;
            var weights = new float[pool.Count];

            for (int i = 0; i < pool.Count; i++)
            {
                weights[i] = GetBreakdownWeight(pool[i]);
                totalWeight += weights[i];
            }

            if (totalWeight <= 0f) return pool[UnityEngine.Random.Range(0, pool.Count)];

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative) return pool[i];
            }

            return pool[pool.Count - 1];
        }

        private float GetBreakdownWeight(BreakdownData breakdown)
        {
            if (breakdown == null || breakdown.requiredParts == null || breakdown.requiredParts.Length == 0)
                return weightCommon;

            PartRarity maxRarity = PartRarity.Common;
            bool found = false;
            foreach (var part in breakdown.requiredParts)
            {
                if (part == null) continue;
                found = true;
                if (part.rarity > maxRarity) maxRarity = part.rarity;
            }

            if (!found) return weightCommon;

            switch (maxRarity)
            {
                case PartRarity.Common:   return weightCommon;
                case PartRarity.Uncommon: return weightUncommon;
                case PartRarity.Rare:     return weightRare;
                case PartRarity.Epic:     return weightEpic;
                default:                  return weightCommon;
            }
        }

        [ContextMenu("ТЕСТ: диагностировать сессию 0")]
        private void TestDiagnose0() => RunDiagnosis(0);

        [ContextMenu("ТЕСТ: симулировать 100 диагностик")]
        private void TestSimulate100()
        {
            if (GarageManager.Instance == null || GarageManager.Instance.Sessions.Count == 0)
            {
                Debug.LogWarning("[DiagnosticManager] Сначала возьми машину в гараж");
                return;
            }

            var session = GarageManager.Instance.Sessions[0];
            var pool = new List<BreakdownData>(session.car.possibleBreakdowns);

            var counter = new Dictionary<string, int>();
            for (int i = 0; i < 100; i++)
            {
                var picked = PickWeighted(pool);
                if (picked == null) continue;
                if (!counter.ContainsKey(picked.displayName)) counter[picked.displayName] = 0;
                counter[picked.displayName]++;
            }

            var sb = new System.Text.StringBuilder("[DiagnosticManager] Симуляция 100:\n");
            foreach (var kv in counter)
                sb.AppendLine($"  {kv.Key}: {kv.Value}%");
            Debug.Log(sb.ToString());
        }
    }
}