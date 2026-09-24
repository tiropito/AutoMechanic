using System;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;
using DG.Tweening;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Показывает только ДЫМ над капотом как декор.
    /// Остальные эффекты (искры, ржавчина, колесо, тормоза) — в DamageIconsPanelUI.
    /// </summary>
    public class CarDamageEffectsUI : MonoBehaviour
    {
        [Header("Только дым (декоративный)")]
        [SerializeField] private Image smokeEffect;
        [SerializeField] private RectTransform carRoot;
        [SerializeField] private float smokeYOffset = 180f;

        private RepairSession _current;
        private Tween _smokeFloatTween;
        private bool _subscribed;

        private void OnEnable() { TrySubscribe(); Refresh(); }
        private void Start() { TrySubscribe(); Refresh(); }
        private void Update() { if (!_subscribed) TrySubscribe(); }
        private void OnDisable() { Unsubscribe(); }
        private void OnDestroy() { Unsubscribe(); _smokeFloatTween?.Kill(); }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (GarageManager.Instance == null || DiagnosticManager.Instance == null) return;

            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnSessionsChanged += Refresh;
                GarageManager.Instance.OnCurrentSessionChanged -= OnSessionChanged;
                GarageManager.Instance.OnCurrentSessionChanged += OnSessionChanged;
            }
            if (DiagnosticManager.Instance != null)
            {
                DiagnosticManager.Instance.OnDiagnosticsUpdated -= OnDiagUpdated;
                DiagnosticManager.Instance.OnDiagnosticsUpdated += OnDiagUpdated;
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnCurrentSessionChanged -= OnSessionChanged;
            }
            if (DiagnosticManager.Instance != null)
                DiagnosticManager.Instance.OnDiagnosticsUpdated -= OnDiagUpdated;
            _subscribed = false;
        }

        private void OnSessionChanged(int _) => Refresh();
        private void OnDiagUpdated(int _) => Refresh();

        public void Refresh()
        {
            if (GarageManager.Instance == null) { HideSmoke(); return; }
            var sessions = GarageManager.Instance.Sessions;
            if (sessions.Count == 0) { HideSmoke(); return; }

            int idx = GarageManager.Instance.CurrentSessionIndex;
            if (idx < 0 || idx >= sessions.Count) { HideSmoke(); return; }

            _current = sessions[idx];

            bool hasSmoke = HasBreakdownWithVisual(idx, BreakdownVisual.Smoke);
            ToggleSmoke(hasSmoke);
        }

        private bool HasBreakdownWithVisual(int sessionIdx, BreakdownVisual visual)
        {
            if (_current == null || _current.brokenDownList == null) return false;
            if (DiagnosticManager.Instance == null) return false;

            foreach (var bd in _current.brokenDownList)
            {
                if (bd == null) continue;
                if (bd.visual != visual) continue;
                if (!DiagnosticManager.Instance.IsFixed(sessionIdx, bd)) return true;
            }
            return false;
        }

        private void ToggleSmoke(bool show)
        {
            if (smokeEffect == null) return;
            bool wasActive = smokeEffect.gameObject.activeSelf;

            if (show)
            {
                smokeEffect.gameObject.SetActive(true);
                var c = smokeEffect.color; c.a = 1f;
                smokeEffect.color = c;

                // Позиционируем дым над капотом — фиксированно сверху
                var rt = smokeEffect.rectTransform;
                rt.anchoredPosition = new Vector2(-180f, smokeYOffset);

                if (!wasActive) StartSmokeFloat();
            }
            else if (wasActive)
            {
                smokeEffect.DOKill();
                _smokeFloatTween?.Kill();
                smokeEffect.gameObject.SetActive(false);
            }
        }

        private void HideSmoke()
        {
            if (smokeEffect != null) smokeEffect.gameObject.SetActive(false);
            _smokeFloatTween?.Kill();
        }

        private void StartSmokeFloat()
        {
            if (smokeEffect == null) return;
            _smokeFloatTween?.Kill();

            var rt = smokeEffect.rectTransform;
            Vector2 basePos = rt.anchoredPosition;
            rt.localScale = Vector3.one * 0.7f;

            _smokeFloatTween = DOTween.Sequence()
                .Append(rt.DOAnchorPos(basePos + new Vector2(0, 40f), 1.5f).SetEase(Ease.OutQuad))
                .Join(rt.DOScale(1.2f, 1.5f).SetEase(Ease.OutQuad))
                .Join(smokeEffect.DOFade(0.2f, 1.5f))
                .Append(rt.DOAnchorPos(basePos, 0.01f))
                .Join(rt.DOScale(0.7f, 0.01f))
                .Join(smokeEffect.DOFade(0.8f, 0.01f))
                .SetLoops(-1);
        }
    }
}