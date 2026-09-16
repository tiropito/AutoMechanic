using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Панель диагностики. Показывает поломки текущей сессии, даёт кнопки
    /// «Диагностика» и «Завершить заказ». Клик по строке → ремонт.
    /// </summary>
    public class DiagnosticPanelUI : MonoBehaviour
    {
        [Header("Корень панели (что скрывать, когда машин нет)")]
        [SerializeField] private GameObject rootPanel;

        [Header("Тексты")]
        [SerializeField] private TMP_Text carNameText;
        [SerializeField] private TMP_Text hintText;

        [Header("Список поломок")]
        [SerializeField] private Transform rowsContainer;
        [SerializeField] private BreakdownRowUI rowPrefab;

        [Header("Кнопки")]
        [SerializeField] private Button diagnoseButton;
        [SerializeField] private TMP_Text diagnoseButtonText;
        [SerializeField] private Button completeButton;
        [SerializeField] private TMP_Text completeButtonText;

        private readonly List<BreakdownRowUI> _rows = new List<BreakdownRowUI>();
        private int _currentSession = 0;

        private void Start()
        {
            if (diagnoseButton != null) diagnoseButton.onClick.AddListener(OnDiagnoseClicked);
            if (completeButton != null) completeButton.onClick.AddListener(OnCompleteClicked);

            if (GarageManager.Instance != null)
                GarageManager.Instance.OnSessionsChanged += Refresh;
            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.OnDiagnosticsUpdated += OnDiagnosticsUpdated;

            Refresh();
        }

        private void OnDestroy()
        {
            if (GarageManager.Instance != null)
                GarageManager.Instance.OnSessionsChanged -= Refresh;
            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.OnDiagnosticsUpdated -= OnDiagnosticsUpdated;
        }

        private void OnDiagnosticsUpdated(int _) => Refresh();

        private void OnDiagnoseClicked()
        {
            if (DiagnosticManager.Instance == null) return;
            DiagnosticManager.Instance.RunDiagnosis(_currentSession);
        }

        private void OnCompleteClicked()
        {
            if (RepairManager.Instance == null) return;
            RepairManager.Instance.TryCompleteOrder(_currentSession);
        }

        private void OnRowClicked(int sessionIndex, BreakdownData breakdown)
        {
            if (RepairManager.Instance == null) return;

            bool ok = RepairManager.Instance.TryRepair(sessionIndex, breakdown);
            if (!ok)
            {
                foreach (var r in _rows)
                    if (r != null && r.Breakdown == breakdown) r.FlashFail();
            }
        }

        /// <summary>Пересобрать всю панель</summary>
        public void Refresh()
        {
            // Убираем старые строки
            foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
            _rows.Clear();

            if (GarageManager.Instance == null || DiagnosticManager.Instance == null)
            {
                if (rootPanel != null) rootPanel.SetActive(false);
                return;
            }

            var sessions = GarageManager.Instance.Sessions;

            // Если машин нет — прячем всю панель
            if (sessions.Count == 0)
            {
                if (rootPanel != null) rootPanel.SetActive(false);
                return;
            }

            if (rootPanel != null) rootPanel.SetActive(true);

            // Защита от неверного индекса
            if (_currentSession >= sessions.Count)
                _currentSession = Mathf.Max(0, sessions.Count - 1);

            var session = sessions[_currentSession];

            if (carNameText != null) carNameText.text = session.car.displayName;

            bool diagnosed = session.state != RepairState.NotDiagnosed;

            // Кнопка «Диагностика»
            if (diagnoseButton != null)
                diagnoseButton.interactable = !diagnosed && EconomyManager.Instance != null;

            if (diagnoseButtonText != null)
                diagnoseButtonText.text = diagnosed
                    ? "Уже проверено"
                    : $"Диагностика (${DiagnosticManager.Instance.DiagnosisCost})";

            // Ещё не диагностирована — не рисуем список
            if (!diagnosed)
            {
                if (hintText != null)
                    hintText.text = "Нажми «Диагностика», чтобы узнать поломки";
                if (completeButton != null) completeButton.interactable = false;
                if (completeButtonText != null) completeButtonText.text = "Завершить заказ";
                return;
            }

            // Рисуем строки
            foreach (var bd in session.brokenDownList)
            {
                if (bd == null) continue;
                var row = Instantiate(rowPrefab, rowsContainer);
                bool isFixed = DiagnosticManager.Instance.IsFixed(_currentSession, bd);
                row.Bind(_currentSession, bd, OnRowClicked);
                row.RefreshVisual(isFixed);
                _rows.Add(row);
            }

            bool allFixed = DiagnosticManager.Instance.IsSessionComplete(_currentSession);

            if (hintText != null)
                hintText.text = allFixed
                    ? "Всё устранено — можно завершать заказ!"
                    : "Кликни по поломке, чтобы поставить деталь";

            if (completeButton != null)
                completeButton.interactable = allFixed;

            if (completeButtonText != null)
                completeButtonText.text = "Завершить заказ";
        }
    }
}