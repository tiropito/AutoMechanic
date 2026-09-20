using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;

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

        private BreakdownData _breakdown;
        private int _sessionIndex;
        private Action<int, BreakdownData> _onClick;

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

            RefreshVisual();
        }

        private void Update()
        {
            // Таймер тикает каждый кадр — обновляем UI
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (_breakdown == null) return;

            bool isFixed = DiagnosticManager.Instance != null &&
                           DiagnosticManager.Instance.IsFixed(_sessionIndex, _breakdown);
            bool isInstalling = RepairManager.Instance != null &&
                                RepairManager.Instance.IsInstalling(_sessionIndex, _breakdown);

            if (isFixed)
            {
                if (statusText != null)
                {
                    statusText.text = "OK";
                    statusText.color = new Color(0.2f, 0.8f, 0.2f);
                }
                if (background != null) background.color = fixedColor;
                if (progressFill != null) progressFill.fillAmount = 1f;
                if (clickButton != null) clickButton.interactable = false;
            }
            else if (isInstalling)
            {
                float left = RepairManager.Instance.GetInstallTimeLeft(_sessionIndex, _breakdown);
                float progress = RepairManager.Instance.GetInstallProgress(_sessionIndex, _breakdown);

                if (statusText != null)
                {
                    statusText.text = $"~ {Mathf.CeilToInt(left)}с";
                    statusText.color = new Color(1f, 0.8f, 0.2f);
                }
                if (background != null) background.color = installingColor;
                if (progressFill != null) progressFill.fillAmount = progress;
                if (clickButton != null) clickButton.interactable = false;
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "X";
                    statusText.color = new Color(0.9f, 0.3f, 0.3f);
                }
                if (background != null) background.color = normalColor;
                if (progressFill != null) progressFill.fillAmount = 0f;
                if (clickButton != null) clickButton.interactable = true;
            }
        }

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
    }
}