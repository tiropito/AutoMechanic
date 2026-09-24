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

        public event Action<int> OnDiagnosticsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

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

            // Поломки уже назначены в GarageManager.TakeCarFromSlot.
            // Здесь только оплата + смена state.
            session.state = RepairState.Diagnosed;

            Debug.Log($"[DiagnosticManager] Сессия {sessionIndex}: диагностика за ${cost}, поломок: {session.brokenDownList.Count}");
            foreach (var bd in session.brokenDownList)
                Debug.Log($"   → {bd.displayName}");

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
            Debug.Log($"[DiagnosticManager] Починено: {breakdown.displayName}");

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

        [ContextMenu("ТЕСТ: диагностировать сессию 0")]
        private void TestDiagnose0() => RunDiagnosis(0);
    }
}