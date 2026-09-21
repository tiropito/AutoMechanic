using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Меняет фон гаража в зависимости от количества постов ремонта (1, 2 или 3).
    /// </summary>
    public class GarageBackgroundSwitcher : MonoBehaviour
    {
        [Header("Фоны (по количеству постов)")]
        [Tooltip("Фон для 1 поста (стартовый)")]
        [SerializeField] private Sprite bay1Background;

        [Tooltip("Фон для 2 постов (после первого апгрейда)")]
        [SerializeField] private Sprite bay2Background;

        [Tooltip("Фон для 3 постов (после второго апгрейда)")]
        [SerializeField] private Sprite bay3Background;

        [Header("Ссылки")]
        [SerializeField] private Image backgroundImage;

        [Header("Настройки")]
        [SerializeField] private float targetAlpha = 0.8f;

        private void Start()
        {
            Subscribe();
            ApplyImmediate();
        }

        private void OnDestroy()
        {
            if (GarageManager.Instance != null)
                GarageManager.Instance.OnSessionsChanged -= OnSessionsChanged;
        }

        private void Subscribe()
        {
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= OnSessionsChanged;
                GarageManager.Instance.OnSessionsChanged += OnSessionsChanged;
            }
        }

        private void OnSessionsChanged() => ApplyImmediate();

        private void ApplyImmediate()
        {
            if (backgroundImage == null) return;

            var sprite = GetTargetBackground();
            if (sprite != null) backgroundImage.sprite = sprite;

            var c = backgroundImage.color;
            c.a = targetAlpha;
            backgroundImage.color = c;
        }

        private Sprite GetTargetBackground()
        {
            if (GarageManager.Instance == null) return bay1Background;

            int bays = GarageManager.Instance.MaxConcurrentRepairs;
            if (bays >= 3) return bay3Background;
            if (bays >= 2) return bay2Background;
            return bay1Background;
        }
    }
}