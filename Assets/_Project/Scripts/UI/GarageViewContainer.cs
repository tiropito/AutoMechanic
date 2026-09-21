using UnityEngine;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Контейнер, который включает нужное количество GarageView (по числу постов ремонта).
    /// </summary>
    public class GarageViewContainer : MonoBehaviour
    {
        [Tooltip("Сколько постов максимум (3)")]
        [SerializeField] private int maxBays = 3;

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            if (GarageManager.Instance != null)
                GarageManager.Instance.OnSessionsChanged -= Refresh;
        }

        private void Subscribe()
        {
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= Refresh;
                GarageManager.Instance.OnSessionsChanged += Refresh;
            }
        }

        private void Refresh()
        {
            if (GarageManager.Instance == null) return;

            int activeBays = Mathf.Clamp(GarageManager.Instance.MaxConcurrentRepairs, 1, maxBays);

            for (int i = 0; i < transform.childCount; i++)
            {
                bool shouldBeActive = i < activeBays;
                var child = transform.GetChild(i).gameObject;
                if (child.activeSelf != shouldBeActive)
                    child.SetActive(shouldBeActive);
            }
        }
    }
}