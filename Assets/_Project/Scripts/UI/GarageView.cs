using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Показывает активную машину из гаража и её фон (по индексу поста).
    /// Меняется при переключении таба.
    /// </summary>
    public class GarageView : MonoBehaviour
    {
        [Header("Фоны для каждого поста")]
        [Tooltip("Фон для 1-го поста (bayIndex = 0)")]
        [SerializeField] private Sprite bay1Background;

        [Tooltip("Фон для 2-го поста (bayIndex = 1)")]
        [SerializeField] private Sprite bay2Background;

        [Tooltip("Фон для 3-го поста (bayIndex = 2)")]
        [SerializeField] private Sprite bay3Background;

        [Header("Ссылки")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image carSprite;
        [SerializeField] private TMPro.TMP_Text carNameText;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnCurrentSessionChanged -= OnSessionSwitched;
            }
        }

        private void Start()
        {
            Subscribe();
            Refresh();
        }

        private void OnDestroy()
        {
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnCurrentSessionChanged -= OnSessionSwitched;
            }
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
        }

        private void OnSessionSwitched(int _) => Refresh();

        public void Refresh()
        {
            Subscribe();

            if (GarageManager.Instance == null) return;

            var sessions = GarageManager.Instance.Sessions;

            // Нет машин в ремонте — скрываем всё
            if (sessions.Count == 0)
            {
                HideAll();
                return;
            }

            int current = GarageManager.Instance.CurrentSessionIndex;
            if (current < 0 || current >= sessions.Count) current = 0;

            var session = sessions[current];

            // === Фон по индексу поста ===
            if (backgroundImage != null)
            {
                var bg = GetBackgroundForBay(current);
                if (bg != null) backgroundImage.sprite = bg;

                var bgColor = backgroundImage.color;
                bgColor.a = 1f;
                backgroundImage.color = bgColor;
                backgroundImage.enabled = true;
            }

            // === Машина ===
            if (session == null || session.car == null)
            {
                if (carSprite != null)
                {
                    carSprite.sprite = null;
                    carSprite.enabled = false;
                    carSprite.color = new Color(1, 1, 1, 0);
                }
                if (carNameText != null) carNameText.gameObject.SetActive(false);
                return;
            }

            bool hasSprite = session.car.sprite != null;
            if (carSprite != null)
            {
                carSprite.sprite = hasSprite ? session.car.sprite : null;
                carSprite.enabled = hasSprite;
                carSprite.color = hasSprite ? Color.white : new Color(1, 1, 1, 0);
            }

            if (carNameText != null)
            {
                carNameText.text = session.car.displayName;
                carNameText.color = session.car.RarityColor;
                carNameText.gameObject.SetActive(true);
            }
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
                var c = backgroundImage.color;
                c.a = 0f;
                backgroundImage.color = c;
            }
            if (carSprite != null)
            {
                carSprite.sprite = null;
                carSprite.enabled = false;
                carSprite.color = new Color(1, 1, 1, 0);
            }
            if (carNameText != null)
            {
                carNameText.text = "";
                carNameText.gameObject.SetActive(false);
            }
        }
    }
}