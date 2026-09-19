using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Показывает спрайт активной машины в центре экрана.
    /// Скрывается, когда открыты модальные панели (магазин, апгрейды).
    /// </summary>
    public class GarageView : MonoBehaviour
    {
        [SerializeField] private Image carSprite;
        [SerializeField] private TMPro.TMP_Text carNameText;

        private void OnEnable()
        {
            ShopPanelUI.OnShopToggled += OnPanelToggled;
            UpgradePanelUI.OnUpgradeToggled += OnPanelToggled;
        }

        private void OnDisable()
        {
            ShopPanelUI.OnShopToggled -= OnPanelToggled;
            UpgradePanelUI.OnUpgradeToggled -= OnPanelToggled;
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

        /// <summary>Скрываем/показываем машину при открытии модальных панелей.</summary>
        private void OnPanelToggled(bool isOpen)
        {
            if (carSprite != null) carSprite.enabled = !isOpen;
            if (carNameText != null) carNameText.enabled = !isOpen;
        }

        public void Refresh()
        {
            Subscribe();
            if (GarageManager.Instance == null || carSprite == null) return;

            var sessions = GarageManager.Instance.Sessions;

            if (sessions.Count == 0)
            {
                carSprite.enabled = false;
                if (carNameText != null) carNameText.text = "";
                return;
            }

            int current = GarageManager.Instance.CurrentSessionIndex;
            var session = sessions[current];

            if (session == null || session.car == null)
            {
                carSprite.enabled = false;
                return;
            }

            bool hasSprite = session.car.sprite != null;
            carSprite.sprite = hasSprite ? session.car.sprite : null;
            carSprite.enabled = hasSprite;

            if (carNameText != null)
                carNameText.text = session.car.displayName;
        }
    }
}