using System;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Gameplay;
using DG.Tweening;

namespace AutoMechanic.UI
{
    public class GarageView : MonoBehaviour
    {
        [Header("Фоны гаража по постам")]
        [SerializeField] private Sprite bay1Background;
        [SerializeField] private Sprite bay2Background;
        [SerializeField] private Sprite bay3Background;

        [Header("Ссылки")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image carSprite;
        [SerializeField] private TMPro.TMP_Text carNameText;

        [Header("Фоновые машины")]
        [SerializeField] private Image backgroundCarLeft;
        [SerializeField] private Button backgroundCarLeftButton;
        [SerializeField] private Image backgroundCarRight;
        [SerializeField] private Button backgroundCarRightButton;

        [Header("Настройки фоновых машин")]
        [Range(0.1f, 0.9f)] [SerializeField] private float backgroundScale = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float backgroundAlpha = 0.9f;
        [Range(0f, 1f)] [SerializeField] private float backgroundDarkness = 0.9f;

        [Header("Анимация машины (въезд)")]
        [SerializeField] private float carAppearDuration = 1.2f;
        [SerializeField] private float carStartOffsetX = 900f;

        [Header("Анимация машины (выезд)")]
        [SerializeField] private float carLeaveDuration = 1.4f;
        [SerializeField] private float carLeaveOffsetX = -1200f;

        [Header("Качение")]
        [SerializeField] private float rockAngle = 1.2f;
        [SerializeField] private float rockSpeed = 14f;
        [SerializeField] private float bobHeight = 4f;
        [SerializeField] private float bobSpeed = 18f;

        private int _leftSessionIndex = -1;
        private int _rightSessionIndex = -1;
        private string _lastCarId;

        private Tween _carTween;
        private Tween _fadeTween;
        private Tween _rockTween;
        private Tween _bobTween;

        private Vector2 _carOriginalPos;
        private bool _carOriginalPosCached;

        private void OnEnable() { Subscribe(); Refresh(); }
        private void OnDisable() { Unsubscribe(); }

        private void OnDestroy()
        {
            Unsubscribe();
            KillCarTweens();
        }

        private void CacheCarOriginalPos()
        {
            if (_carOriginalPosCached || carSprite == null) return;
            _carOriginalPos = carSprite.rectTransform.anchoredPosition;
            _carOriginalPosCached = true;
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
            if (backgroundCarLeftButton != null)
            {
                backgroundCarLeftButton.onClick.RemoveAllListeners();
                backgroundCarLeftButton.onClick.AddListener(OnBackgroundLeftClicked);
            }
            if (backgroundCarRightButton != null)
            {
                backgroundCarRightButton.onClick.RemoveAllListeners();
                backgroundCarRightButton.onClick.AddListener(OnBackgroundRightClicked);
            }
        }

        private void Unsubscribe()
        {
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnCurrentSessionChanged -= OnSessionSwitched;
            }
            if (backgroundCarLeftButton != null) backgroundCarLeftButton.onClick.RemoveAllListeners();
            if (backgroundCarRightButton != null) backgroundCarRightButton.onClick.RemoveAllListeners();
        }

        private void OnSessionSwitched(int _) => Refresh();

        private void OnBackgroundLeftClicked()
        {
            if (_leftSessionIndex >= 0 && GarageManager.Instance != null)
                GarageManager.Instance.SelectSession(_leftSessionIndex);
        }

        private void OnBackgroundRightClicked()
        {
            if (_rightSessionIndex >= 0 && GarageManager.Instance != null)
                GarageManager.Instance.SelectSession(_rightSessionIndex);
        }

        public void Refresh()
        {
            Subscribe();
            CacheCarOriginalPos();

            if (GarageManager.Instance == null) return;
            var sessions = GarageManager.Instance.Sessions;
            if (sessions.Count == 0) { HideAll(); return; }

            int current = GarageManager.Instance.CurrentSessionIndex;
            if (current < 0 || current >= sessions.Count) current = 0;

            var session = sessions[current];

            if (backgroundImage != null)
            {
                var bg = GetBackgroundForBay(current);
                if (bg != null) backgroundImage.sprite = bg;
                var c = backgroundImage.color; c.a = 1f;
                backgroundImage.color = c;
                backgroundImage.enabled = true;
            }

            if (session != null && session.car != null)
            {
                bool hasSprite = session.car.sprite != null;
                string currentCarId = session.car.id;
                bool carChanged = currentCarId != _lastCarId;

                if (carSprite != null)
                {
                    carSprite.sprite = hasSprite ? session.car.sprite : null;
                    carSprite.enabled = hasSprite;
                    carSprite.color = hasSprite ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0);
                    float scale = session.car.spriteScale > 0f ? session.car.spriteScale : 1f;
                    carSprite.rectTransform.localScale = Vector3.one * scale;
                }

                if (carNameText != null)
                {
                    carNameText.text = session.car.displayName;
                    carNameText.color = session.car.RarityColor;
                    carNameText.gameObject.SetActive(true);
                }

                if (carChanged)
                {
                    _lastCarId = currentCarId;
                    PlayCarAppearAnimation();
                }
            }
            else
            {
                if (carSprite != null) { carSprite.enabled = false; carSprite.color = new Color(1, 1, 1, 0); }
                if (carNameText != null) carNameText.gameObject.SetActive(false);
                _lastCarId = null;
                KillCarTweens();
            }

            UpdateBackgroundCars(sessions, current);
        }

        private void UpdateBackgroundCars(System.Collections.Generic.IReadOnlyList<RepairSession> sessions, int current)
        {
            RepairSession leftSession = null, rightSession = null;
            int leftIndex = -1, rightIndex = -1, counter = 0;

            for (int i = 0; i < sessions.Count; i++)
            {
                if (i == current) continue;
                if (sessions[i] == null || sessions[i].car == null) continue;
                if (sessions[i].car.sprite == null) continue;

                if (counter == 0) { leftSession = sessions[i]; leftIndex = i; }
                else if (counter == 1) { rightSession = sessions[i]; rightIndex = i; }
                counter++;
            }

            ApplyBackgroundCar(backgroundCarLeft, backgroundCarLeftButton, leftSession);
            ApplyBackgroundCar(backgroundCarRight, backgroundCarRightButton, rightSession);

            _leftSessionIndex = leftIndex;
            _rightSessionIndex = rightIndex;
        }

        private void ApplyBackgroundCar(Image target, Button button, RepairSession session)
        {
            if (target == null) return;

            if (session == null || session.car == null || session.car.sprite == null)
            {
                target.sprite = null;
                target.enabled = false;
                target.color = new Color(1, 1, 1, 0);
                if (button != null) button.interactable = false;
                return;
            }

            target.sprite = session.car.sprite;
            target.enabled = true;
            target.raycastTarget = true;

            float dark = Mathf.Clamp01(backgroundDarkness);
            target.color = new Color(dark, dark, dark, backgroundAlpha);

            float carScale = session.car.spriteScale > 0f ? session.car.spriteScale : 1f;
            target.rectTransform.localScale = Vector3.one * (backgroundScale * carScale);

            if (button != null) button.interactable = true;
        }

        private void PlayCarAppearAnimation()
        {
            if (carSprite == null) return;

            KillCarTweens();
            CacheCarOriginalPos();

            var rt = carSprite.rectTransform;
            rt.localRotation = Quaternion.identity;

            Vector2 startPos = _carOriginalPos + new Vector2(carStartOffsetX, 0f);
            rt.anchoredPosition = startPos;

            var c = carSprite.color;
            c.a = 0f;
            carSprite.color = c;

            _carTween = rt.DOAnchorPos(_carOriginalPos, carAppearDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(StartIdleRock);

            _fadeTween = carSprite.DOFade(1f, carAppearDuration * 0.6f).SetEase(Ease.OutQuad);

            rt.localRotation = Quaternion.Euler(0, 0, -rockAngle);
            _rockTween = rt.DOLocalRotate(new Vector3(0, 0, rockAngle), rockSpeed * 0.05f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public void PlayCarLeaveAnimation(Action onComplete = null)
        {
            if (carSprite == null || !carSprite.enabled)
            {
                onComplete?.Invoke();
                return;
            }

            KillCarTweens();
            CacheCarOriginalPos();

            var rt = carSprite.rectTransform;
            rt.anchoredPosition = _carOriginalPos;
            rt.localRotation = Quaternion.identity;

            Vector2 targetPos = _carOriginalPos + new Vector2(carLeaveOffsetX, 0f);

            rt.localRotation = Quaternion.Euler(0, 0, rockAngle);
            _rockTween = rt.DOLocalRotate(new Vector3(0, 0, -rockAngle), carLeaveDuration * 0.15f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            _carTween = rt.DOAnchorPos(targetPos, carLeaveDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    _rockTween?.Kill();
                    rt.localRotation = Quaternion.identity;
                    rt.anchoredPosition = _carOriginalPos;
                    carSprite.color = new Color(1, 1, 1, 0);
                    _lastCarId = null;
                    onComplete?.Invoke();
                });

            _fadeTween = carSprite.DOFade(0f, carLeaveDuration * 0.7f)
                .SetDelay(carLeaveDuration * 0.3f)
                .SetEase(Ease.InQuad);
        }

        private void StartIdleRock()
        {
            _rockTween?.Kill();
            _bobTween?.Kill();

            var rt = carSprite.rectTransform;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = _carOriginalPos;

            _bobTween = rt.DOAnchorPos(_carOriginalPos + new Vector2(0, bobHeight), 1.2f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        private void KillCarTweens()
        {
            _carTween?.Kill();
            _fadeTween?.Kill();
            _rockTween?.Kill();
            _bobTween?.Kill();
            if (carSprite != null) carSprite.rectTransform.localRotation = Quaternion.identity;
        }

        private Sprite GetBackgroundForBay(int bayIndex)
        {
            switch (bayIndex)
            {
                case 0: return bay1Background;
                case 1: return bay2Background;
                case 2: return bay3Background;
                default: return bay1Background;
            }
        }

        private void HideAll()
        {
            if (backgroundImage != null)
            {
                if (bay1Background != null) backgroundImage.sprite = bay1Background;
                var c = backgroundImage.color; c.a = 1f;
                backgroundImage.color = c;
                backgroundImage.enabled = true;
            }

            KillCarTweens();

            if (carSprite != null) { carSprite.sprite = null; carSprite.enabled = false; carSprite.color = new Color(1, 1, 1, 0); }
            if (carNameText != null) { carNameText.text = ""; carNameText.gameObject.SetActive(false); }
            if (backgroundCarLeft != null) { backgroundCarLeft.sprite = null; backgroundCarLeft.enabled = false; backgroundCarLeft.color = new Color(1, 1, 1, 0); }
            if (backgroundCarRight != null) { backgroundCarRight.sprite = null; backgroundCarRight.enabled = false; backgroundCarRight.color = new Color(1, 1, 1, 0); }
            if (backgroundCarLeftButton != null) backgroundCarLeftButton.interactable = false;
            if (backgroundCarRightButton != null) backgroundCarRightButton.interactable = false;
            _leftSessionIndex = -1;
            _rightSessionIndex = -1;
            _lastCarId = null;
        }
    }
}