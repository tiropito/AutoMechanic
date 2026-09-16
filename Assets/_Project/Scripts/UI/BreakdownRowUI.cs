using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Одна строка в списке поломок.
    /// Клик по строке → панель ловит событие и пытается починить.
    /// </summary>
    public class BreakdownRowUI : MonoBehaviour
    {
        [Header("Ссылки на дочерние объекты")]
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button clickButton;

        [Header("Цвета")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color fixedColor = new Color(0.5f, 1f, 0.5f, 0.25f);
        [SerializeField] private Color failFlashColor = new Color(1f, 0.4f, 0.4f, 0.6f);

        private BreakdownData _breakdown;
        private int _sessionIndex;
        private Action<int, BreakdownData> _onClick;

        public BreakdownData Breakdown => _breakdown;

        /// <summary>Навесить данные на строку</summary>
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
        }

        /// <summary>Обновить визуал (✅/❌ и цвет фона)</summary>
        public void RefreshVisual(bool isFixed)
        {
            if (statusText != null) statusText.text = isFixed ? "✅" : "❌";
            if (background != null) background.color = isFixed ? fixedColor : normalColor;
        }

        /// <summary>Мигнуть красным, если детали нет в инвентаре</summary>
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
            background.color = normalColor;
        }
    }
}