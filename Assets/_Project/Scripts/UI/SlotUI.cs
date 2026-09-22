using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;
using DG.Tweening;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Карточка слота. Анимация появления + пульсация для бонусных.
    /// </summary>
    public class SlotUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private Image background;
        [SerializeField] private Image carImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject bonusMark;
        [SerializeField] private TMP_Text refillText;
        [SerializeField] private Button clickButton;

        [Header("Цвета")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color bonusColor = new Color(1f, 0.85f, 0.3f, 0.25f);
        [SerializeField] private Color emptyColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

        [Header("Цвета таймера")]
        [SerializeField] private Color timerNormal = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private Color timerDanger = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color timerRefill = new Color(0.7f, 0.7f, 0.7f);

        [Header("Анимации")]
        [SerializeField] private float appearDuration = 0.4f;
        [SerializeField] private float bonusPulseScale = 1.04f;
        [SerializeField] private float bonusPulseDuration = 0.8f;

        private int _slotIndex;
        private Action<int> _onClick;
        private SlotData _slot;

        private Tween _pulseTween;
        private Tween _carPopTween;
        private bool _wasBonus;
        private bool _wasHasCar;
        private bool _wasInitialized;

        public int SlotIndex => _slotIndex;

        public void Bind(int slotIndex, Action<int> onClick)
        {
            _slotIndex = slotIndex;
            _onClick = onClick;

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(() => _onClick?.Invoke(_slotIndex));
            }

            // Анимация появления
            PlayAppearAnimation();
        }

        private void PlayAppearAnimation()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            // Начальное состояние: уменьшен и прозрачен
            rt.localScale = Vector3.one * 0.85f;

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            // Анимация scale + fade
            rt.DOScale(1f, appearDuration).SetEase(Ease.OutBack);
            canvasGroup.DOFade(1f, appearDuration).SetEase(Ease.OutQuad);
        }

        private void PlayCarAppearPop()
        {
            if (carImage == null) return;

            var rt = carImage.rectTransform;
            _carPopTween?.Kill();
            rt.localScale = Vector3.one;

            _carPopTween = rt.DOPunchScale(Vector3.one * 0.25f, 0.35f, 8, 0.8f);
        }

        public void Refresh(SlotData slot, bool canTake)
        {
            _slot = slot;
            if (_slot == null) return;

            bool hasCar = slot.currentCar != null;

            if (carImage != null)
            {
                bool hasSprite = hasCar && slot.currentCar.sprite != null;
                carImage.sprite = hasSprite ? slot.currentCar.sprite : null;
                carImage.enabled = hasSprite;
            }

            if (nameText != null)
            {
                nameText.text = hasCar ? slot.currentCar.displayName : "...";
                nameText.color = hasCar ? slot.currentCar.RarityColor : new Color(0.6f, 0.6f, 0.6f);
            }

            if (bonusMark != null)
                bonusMark.SetActive(hasCar && slot.isBonus);

            if (background != null)
            {
                if (!hasCar) background.color = emptyColor;
                else if (slot.isBonus) background.color = bonusColor;
                else background.color = normalColor;
            }

            if (clickButton != null)
                clickButton.interactable = hasCar && canTake;

            // Управление пульсацией
            // Pop при появлении новой машины (не при первом Refresh)
            bool hasCarNow = hasCar;
            if (hasCarNow && !_wasHasCar && _wasInitialized)
                PlayCarAppearPop();

            _wasHasCar = hasCarNow;

            bool isBonusNow = hasCar && slot.isBonus;
            if (isBonusNow && !_wasBonus) StartPulse();
            else if (!isBonusNow && _wasBonus) StopPulse();
            _wasBonus = isBonusNow;

            _wasInitialized = true;

            UpdateTimerText();
        }

        private void StartPulse()
        {
            StopPulse();
            var rt = GetComponent<RectTransform>();
            if (rt == null) return;

            _pulseTween = rt.DOScale(bonusPulseScale, bonusPulseDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void StopPulse()
        {
            _pulseTween?.Kill();
            _pulseTween = null;

            var rt = GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            StopPulse();
            _carPopTween?.Kill();
        }

        private void Update()
        {
            UpdateTimerText();
        }

        private void UpdateTimerText()
        {
            if (refillText == null || _slot == null) return;

            if (_slot.currentCar != null)
            {
                if (_slot.isBonus)
                {
                    float t = Mathf.Max(0f, _slot.bonusTimeLeft);
                    int min = Mathf.FloorToInt(t / 60f);
                    int sec = Mathf.FloorToInt(t % 60f);
                    refillText.text = $"* {min}:{sec:00}";
                    refillText.color = t <= 30f ? timerDanger : timerNormal;
                }
                else
                {
                    refillText.text = "";
                }
            }
            else
            {
                float left = Mathf.Max(0f, _slot.refillDelay - _slot.timeSinceEmpty);
                refillText.text = $"{left:F1} с";
                refillText.color = timerRefill;
            }
        }
    }
}