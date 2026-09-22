using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;
using DG.Tweening;

namespace AutoMechanic.UI
{
    public class DiagnosticPanelUI : MonoBehaviour
    {
        [Header("Корень панели")]
        [SerializeField] private GameObject rootPanel;

        [Header("Табы выбора машины")]
        [SerializeField] private Transform tabsContainer;
        [SerializeField] private SessionTabUI tabPrefab;

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

        [Header("Кнопка «Продать как есть»")]
        [SerializeField] private Button sellAsIsButton;
        [SerializeField] private TMP_Text sellAsIsButtonText;

        private readonly List<BreakdownRowUI> _rows = new List<BreakdownRowUI>();
        private readonly List<SessionTabUI> _tabs = new List<SessionTabUI>();
        private Tween _completePulseTween;

        private void Start()
        {
            if (diagnoseButton != null) diagnoseButton.onClick.AddListener(OnDiagnoseClicked);
            if (completeButton != null) completeButton.onClick.AddListener(OnCompleteClicked);
            if (sellAsIsButton != null) sellAsIsButton.onClick.AddListener(OnSellAsIsClicked);

            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnSessionsChanged += Refresh;
                GarageManager.Instance.OnCurrentSessionChanged -= OnSessionSwitched;
                GarageManager.Instance.OnCurrentSessionChanged += OnSessionSwitched;
            }
            if (DiagnosticManager.Instance != null)
            {
                DiagnosticManager.Instance.OnDiagnosticsUpdated -= OnDiagnosticsUpdated;
                DiagnosticManager.Instance.OnDiagnosticsUpdated += OnDiagnosticsUpdated;
            }
        }

        private void OnDestroy()
        {
            _completePulseTween?.Kill();

            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnCurrentSessionChanged -= OnSessionSwitched;
            }
            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.OnDiagnosticsUpdated -= OnDiagnosticsUpdated;
        }

        private void OnDiagnosticsUpdated(int _) => Refresh();
        private void OnSessionSwitched(int _) => Refresh();

        private void OnDiagnoseClicked()
        {
            if (DiagnosticManager.Instance == null) return;
            DiagnosticManager.Instance.RunDiagnosis(GetCurrentIndex());
        }

        private void OnCompleteClicked()
        {
            if (RepairManager.Instance == null) return;
            RepairManager.Instance.TryCompleteOrder(GetCurrentIndex());
        }

        private void OnSellAsIsClicked()
        {
            if (RepairManager.Instance == null) return;
            RepairManager.Instance.TrySellAsIs(GetCurrentIndex());
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

        private int GetCurrentIndex()
        {
            return GarageManager.Instance != null ? GarageManager.Instance.CurrentSessionIndex : 0;
        }

        public void Refresh()
        {
            Subscribe();

            foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
            foreach (var t in _tabs) if (t != null) Destroy(t.gameObject);
            _rows.Clear();
            _tabs.Clear();

            if (GarageManager.Instance == null || DiagnosticManager.Instance == null)
            {
                if (rootPanel != null) rootPanel.SetActive(false);
                return;
            }

            var sessions = GarageManager.Instance.Sessions;

            if (sessions.Count == 0)
            {
                if (rootPanel != null) rootPanel.SetActive(false);
                return;
            }

            if (rootPanel != null) rootPanel.SetActive(true);

            int current = GarageManager.Instance.CurrentSessionIndex;

            // === Табы ===
            if (tabsContainer != null && tabPrefab != null)
            {
                for (int i = 0; i < sessions.Count; i++)
                {
                    var tab = Instantiate(tabPrefab, tabsContainer);
                    int idx = i;
                    tab.Bind(idx, OnTabClicked);
                    tab.Refresh(sessions[i].car, i == current);
                    _tabs.Add(tab);
                }
            }

            var session = sessions[current];

            if (carNameText != null)
            {
                carNameText.text = session.car.displayName;
                carNameText.color = session.car.RarityColor;
            }

            bool diagnosed = session.state != RepairState.NotDiagnosed;

            // === Стоимость диагностики по редкости машины ===
            int diagCost = DiagnosticManager.Instance.GetDiagnosisCost(current);

            if (diagnoseButton != null)
                diagnoseButton.interactable = !diagnosed && EconomyManager.Instance != null;

            if (diagnoseButtonText != null)
                diagnoseButtonText.text = diagnosed
                    ? "Уже проверено"
                    : $"Диагностика (${diagCost})";

            // === Кнопка «Продать как есть» ===
            bool allFixedNow = diagnosed && DiagnosticManager.Instance.IsSessionComplete(current);

            // Кнопка «Продать как есть» — только пока есть непочиненные поломки
            if (sellAsIsButton != null) sellAsIsButton.gameObject.SetActive(!allFixedNow);

            if (sellAsIsButtonText != null && RepairManager.Instance != null)
            {
                int price = RepairManager.Instance.CalculateSellAsIsPrice(current);
                if (price > 0)
                    sellAsIsButtonText.text = $"Продать как есть (+${price})";
                else if (price < 0)
                    sellAsIsButtonText.text = $"Отказаться (${price})";
                else
                    sellAsIsButtonText.text = "Продать как есть";
            }

            if (!diagnosed)
            {
                if (hintText != null)
                    hintText.text = "Нажми «Диагностика», чтобы узнать поломки";
                if (completeButton != null) completeButton.interactable = false;
                if (completeButtonText != null) completeButtonText.text = "Завершить заказ";
                return;
            }

            foreach (var bd in session.brokenDownList)
            {
                if (bd == null) continue;
                var row = Instantiate(rowPrefab, rowsContainer);
                row.Bind(current, bd, OnRowClicked);
                _rows.Add(row);
            }

            bool allFixed = DiagnosticManager.Instance.IsSessionComplete(current);

            if (hintText != null)
                hintText.text = allFixed
                    ? "Всё устранено — можно завершать заказ!"
                    : "Кликни по поломке, чтобы поставить деталь";

            if (completeButton != null)
            {
                completeButton.interactable = allFixed;

                if (allFixed && _completePulseTween == null)
                    StartCompletePulse();
                else if (!allFixed && _completePulseTween != null)
                    StopCompletePulse();
            }

            if (completeButtonText != null)
                completeButtonText.text = "Завершить заказ";
        }

        private void StartCompletePulse()
        {
            if (completeButton == null) return;

            var rt = completeButton.GetComponent<RectTransform>();
            if (rt == null) return;

            _completePulseTween?.Kill();
            rt.localScale = Vector3.one;

            _completePulseTween = rt.DOScale(1.06f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void StopCompletePulse()
        {
            _completePulseTween?.Kill();
            _completePulseTween = null;

            if (completeButton != null)
                completeButton.GetComponent<RectTransform>().localScale = Vector3.one;
        }

        private void OnTabClicked(int index)
        {
            if (GarageManager.Instance != null)
                GarageManager.Instance.SelectSession(index);
        }
    }
}