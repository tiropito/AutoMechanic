using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Показывает активную машину и фоновые машины из других постов.
    /// Фоновые — затемнённые, полупрозрачные, уменьшенные. Кликабельны.
    /// </summary>
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
        [Range(0.1f, 0.9f)]
        [SerializeField] private float backgroundScale = 0.5f;

        [Range(0f, 1f)]
        [SerializeField] private float backgroundAlpha = 0.9f;

        [Range(0f, 1f)]
        [SerializeField] private float backgroundDarkness = 0.9f;

        private int _leftSessionIndex = -1;
        private int _rightSessionIndex = -1;

        private void OnEnable() { Subscribe(); Refresh(); }
        private void OnDisable() { Unsubscribe(); }
        private void Start() { Subscribe(); Refresh(); }
        private void OnDestroy() { Unsubscribe(); }

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

            if (GarageManager.Instance == null) return;

            var sessions = GarageManager.Instance.Sessions;

            if (sessions.Count == 0) { HideAll(); return; }

            int current = GarageManager.Instance.CurrentSessionIndex;
            if (current < 0 || current >= sessions.Count) current = 0;

            var session = sessions[current];

            // Фон гаража
            if (backgroundImage != null)
            {
                var bg = GetBackgroundForBay(current);
                if (bg != null) backgroundImage.sprite = bg;
                var c = backgroundImage.color; c.a = 1f;
                backgroundImage.color = c;
                backgroundImage.enabled = true;
            }

            // Активная машина
            if (session != null && session.car != null)
            {
                bool hasSprite = session.car.sprite != null;
                if (carSprite != null)
                {
                    carSprite.sprite = hasSprite ? session.car.sprite : null;
                    carSprite.enabled = hasSprite;
                    carSprite.color = hasSprite ? Color.white : new Color(1, 1, 1, 0);
                    float scale = session.car.spriteScale > 0f ? session.car.spriteScale : 1f;
                    carSprite.rectTransform.localScale = Vector3.one * scale;
                }
                if (carNameText != null)
                {
                    carNameText.text = session.car.displayName;
                    carNameText.color = session.car.RarityColor;
                    carNameText.gameObject.SetActive(true);
                }
            }
            else
            {
                if (carSprite != null) { carSprite.enabled = false; carSprite.color = new Color(1, 1, 1, 0); }
                if (carNameText != null) carNameText.gameObject.SetActive(false);
            }

            UpdateBackgroundCars(sessions, current);
        }

        private void UpdateBackgroundCars(System.Collections.Generic.IReadOnlyList<RepairSession> sessions, int current)
        {
            RepairSession leftSession = null, rightSession = null;
            int leftIndex = -1, rightIndex = -1;
            int counter = 0;

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
            // Фон гаража НЕ прячем — показываем стартовый
            if (backgroundImage != null)
            {
                if (bay1Background != null) backgroundImage.sprite = bay1Background;
                var c = backgroundImage.color;
                c.a = 1f;
                backgroundImage.color = c;
                backgroundImage.enabled = true;
            }

            // Машины — прячем
            if (carSprite != null) { carSprite.sprite = null; carSprite.enabled = false; carSprite.color = new Color(1, 1, 1, 0); }
            if (carNameText != null) { carNameText.text = ""; carNameText.gameObject.SetActive(false); }
            if (backgroundCarLeft != null) { backgroundCarLeft.sprite = null; backgroundCarLeft.enabled = false; backgroundCarLeft.color = new Color(1,1,1,0); }
            if (backgroundCarRight != null) { backgroundCarRight.sprite = null; backgroundCarRight.enabled = false; backgroundCarRight.color = new Color(1,1,1,0); }
            if (backgroundCarLeftButton != null) backgroundCarLeftButton.interactable = false;
            if (backgroundCarRightButton != null) backgroundCarRightButton.interactable = false;
            _leftSessionIndex = -1;
            _rightSessionIndex = -1;
        }
    }
}