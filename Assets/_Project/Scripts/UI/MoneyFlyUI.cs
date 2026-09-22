using System;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;
using DG.Tweening;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Спавнит летящий текст "+$X" к счётчику денег.
    /// Плюс пульсация счётчика зелёным/красным.
    /// </summary>
    public class MoneyFlyUI : MonoBehaviour
    {
        public static MoneyFlyUI Instance { get; private set; }

        [Header("Префаб летящего текста")]
        [Tooltip("Префаб с MoneyFlyText на борту")]
        [SerializeField] private MoneyFlyText flyPrefab;

        [Header("Куда летит (счётчик денег)")]
        [Tooltip("RectTransform счётчика денег в углу")]
        [SerializeField] private RectTransform moneyTarget;

        [Header("Откуда вылетает (центр гаража)")]
        [Tooltip("RectTransform машины в гараже — оттуда летит +$X")]
        [SerializeField] private RectTransform flyOrigin;

        [Header("Пульсация счётчика")]
        [SerializeField] private float pulseScale = 1.2f;
        [SerializeField] private float pulseDuration = 0.15f;

        [Header("Цвета")]
        [SerializeField] private Color gainColor = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color spendColor = new Color(1f, 0.4f, 0.4f);

        private Canvas _canvas;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _canvas = GetComponentInParent<Canvas>();
        }

        /// <summary>Показать +$X (зелёным).</summary>
        public void ShowReward(int amount)
        {
            if (amount <= 0) return;
            SpawnFly(amount, PulseGain);
        }

        /// <summary>Показать -$X (красным).</summary>
        public void ShowSpend(int amount)
        {
            if (amount <= 0) return;
            SpawnFly(-amount, PulseSpend);
        }

        /// <summary>Универсальный метод: сам определяет знак и цвет.</summary>
        public void ShowAmount(int amount)
        {
            if (amount == 0) return;
            if (amount > 0) ShowReward(amount);
            else ShowSpend(Mathf.Abs(amount));
        }

        private void SpawnFly(int signedAmount, Action onComplete)
        {
            if (flyPrefab == null) { Debug.LogWarning("[MoneyFlyUI] flyPrefab не назначен!"); return; }

            Vector2 startPos = GetCanvasPos(flyOrigin);
            Vector2 endPos = GetCanvasPos(moneyTarget);

            var instance = Instantiate(flyPrefab, transform);
            instance.Fly(signedAmount, startPos, endPos, onComplete);
        }

        private Vector2 GetCanvasPos(RectTransform target)
        {
            if (target == null) return Vector2.zero;

            // Если оба — дети одного Canvas, конвертируем мировую позицию в anchoredPosition
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);

            if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
            var canvasRt = _canvas != null ? _canvas.transform as RectTransform : null;
            if (canvasRt == null) return screenPos;

            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPos, _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out localPos);
            return localPos;
        }

        private void PulseGain()
        {
            if (moneyTarget == null) return;
            moneyTarget.DOKill();
            moneyTarget.localScale = Vector3.one;
            moneyTarget.DOPunchScale(Vector3.one * (pulseScale - 1f), pulseDuration + 0.15f, 6, 0.5f);
        }

        private void PulseSpend()
        {
            if (moneyTarget == null) return;
            moneyTarget.DOKill();
            moneyTarget.localScale = Vector3.one;
            moneyTarget.DOPunchScale(Vector3.one * (pulseScale - 1f), pulseDuration + 0.15f, 6, 0.5f);
        }
    }
}