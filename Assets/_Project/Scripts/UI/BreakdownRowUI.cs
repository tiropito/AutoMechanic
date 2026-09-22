using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;
using DG.Tweening;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Одна строка в списке поломок.
    /// Три состояния: ❌ не починена / ⏳ устанавливается / ✅ починена.
    /// </summary>
    public class BreakdownRowUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button clickButton;

        [Header("Прогресс-бар (опционально)")]
        [Tooltip("Image с Image Type = Filled. Если не задан — прогресс не показывается.")]
        [SerializeField] private Image progressFill;

        [Header("Цвета")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color fixedColor = new Color(0.5f, 1f, 0.5f, 0.25f);
        [SerializeField] private Color installingColor = new Color(1f, 0.85f, 0.3f, 0.25f);
        [SerializeField] private Color failFlashColor = new Color(1f, 0.4f, 0.4f, 0.6f);

        [Header("Цвета статусов")]
        [SerializeField] private Color fixedStatusColor = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color installingStatusColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color notFixedStatusColor = new Color(0.9f, 0.3f, 0.3f);

        [Header("Анимация появления галочки")]
        [SerializeField] private float checkAppearDuration = 0.4f;
        [SerializeField] private float checkPopScale = 1.6f;

        private BreakdownData _breakdown;
        private int _sessionIndex;
        private Action<int, BreakdownData> _onClick;

        // Отслеживание смены состояний
        private bool _wasFixed;
        private bool _wasInstalling;

        public BreakdownData Breakdown => _breakdown;

        public void Bind(int sessionIndex, BreakdownData breakdown, Action<int, BreakdownData> onClick)
        {
            _sessionIndex = sessionIndex;
            _breakdown = breakdown;
            _onClick = onClick;

            if (iconImage != null) iconImage.sprite = breakdown.icon;
            if (nameText != null) nameText.text = breakdown.displayName;

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => _onClick?.Invoke(_sessionIndex, _breakdown));
            }

            // Сброс состояний перед первым Refresh
            _wasFixed = false;
            _wasInstalling = false;

            RefreshVisual();
        }

        private void Update()
        {
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (_breakdown == null) return;

            bool isFixed = DiagnosticManager.Instance != null &&
                           DiagnosticManager.Instance.IsFixed(_sessionIndex, _breakdown);
            bool isInstalling = RepairManager.Instance != null &&
                                RepairManager.Instance.IsInstalling(_sessionIndex, _breakdown);

            // === Определяем, что менять ===
            if (isFixed)
            {
                ApplyFixedVisual();

                // Анимация при переходе в "починено"
                if (!_wasFixed)
                    PlayCheckAppearAnimation();
            }
            else if (isInstalling)
            {
                ApplyInstallingVisual();
            }
            else
            {
                ApplyNotFixedVisual();
            }

            _wasFixed = isFixed;
            _wasInstalling = isInstalling;
        }

        // ==================== ВИЗУАЛЫ СОСТОЯНИЙ ====================

        private void ApplyFixedVisual()
        {
            if (statusText != null)
            {
                statusText.text = "OK";
                statusText.color = fixedStatusColor;
            }
            if (background != null) background.color = fixedColor;
            if (progressFill != null) progressFill.fillAmount = 1f;
            if (clickButton != null) clickButton.interactable = false;
        }

        private void ApplyInstallingVisual()
        {
            float left = RepairManager.Instance.GetInstallTimeLeft(_sessionIndex, _breakdown);
            float progress = RepairManager.Instance.GetInstallProgress(_sessionIndex, _breakdown);

            if (statusText != null)
            {
                statusText.text = $"~ {Mathf.CeilToInt(left)}с";
                statusText.color = installingStatusColor;
            }
            if (background != null) background.color = installingColor;
            if (progressFill != null) progressFill.fillAmount = progress;
            if (clickButton != null) clickButton.interactable = false;
        }

        private void ApplyNotFixedVisual()
        {
            if (statusText != null)
            {
                statusText.text = "X";
                statusText.color = notFixedStatusColor;
            }
            if (background != null) background.color = normalColor;
            if (progressFill != null) progressFill.fillAmount = 0f;
            if (clickButton != null) clickButton.interactable = true;
        }

        // ==================== АНИМАЦИЯ ГАЛОЧКИ ====================

        private void PlayCheckAppearAnimation()
        {
            // Scale-in для текста статуса
            if (statusText != null)
            {
                var rt = statusText.rectTransform;
                rt.DOKill();

                rt.localScale = Vector3.one * 0.3f;
                rt.DOScale(checkPopScale, checkAppearDuration * 0.6f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        rt.DOScale(1f, checkAppearDuration * 0.4f).SetEase(Ease.InQuad);
                    });
            }

            // Пульсация фона
            if (background != null)
            {
                background.DOKill();
                background.color = fixedColor;
                background.DOFade(0f, 0.05f)  // мигание для "вспышки"
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(Ease.Linear);
            }
        }

        // ==================== ОШИБКА ====================

        public void FlashFail()
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            if (background == null) yield break;
            background.color = failFlashColor;
            yield return new WaitForSeconds(0.35f);
            RefreshVisual();
        }

        private void OnDestroy()
        {
            if (statusText != null) statusText.rectTransform.DOKill();
            if (background != null) background.DOKill();
        }
    }
}