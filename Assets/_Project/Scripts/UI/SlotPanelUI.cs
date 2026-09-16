using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Core;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Панель слева со слотами заказов.
    /// Пересобирает список при каждом OnSlotsChanged.
    /// </summary>
    public class SlotPanelUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private SlotUI slotPrefab;

        private readonly List<SlotUI> _rows = new List<SlotUI>();

        private void Start()
        {
            Subscribe();
            Rebuild();
        }

        private void OnDestroy()
        {
            if (SlotManager.Instance != null)
                SlotManager.Instance.OnSlotsChanged -= Rebuild;
            if (GarageManager.Instance != null)
                GarageManager.Instance.OnSessionsChanged -= RefreshVisuals;
        }

        private void Subscribe()
        {
            if (SlotManager.Instance != null)
            {
                SlotManager.Instance.OnSlotsChanged -= Rebuild;
                SlotManager.Instance.OnSlotsChanged += Rebuild;
            }
            if (GarageManager.Instance != null)
            {
                GarageManager.Instance.OnSessionsChanged -= RefreshVisuals;
                GarageManager.Instance.OnSessionsChanged += RefreshVisuals;
            }
        }

        /// <summary>Полная пересборка списка (при изменении количества слотов).</summary>
        public void Rebuild()
        {
            Subscribe();

            if (SlotManager.Instance == null)
            {
                if (rootPanel != null) rootPanel.SetActive(false);
                return;
            }

            if (rootPanel != null) rootPanel.SetActive(true);

            // Если количество слотов не изменилось — только обновим визуал
            if (_rows.Count == SlotManager.Instance.Slots.Count)
            {
                RefreshVisuals();
                return;
            }

            // Пересобираем
            foreach (var r in _rows) if (r != null) Destroy(r.gameObject);
            _rows.Clear();

            if (slotPrefab == null || slotsContainer == null)
            {
                Debug.LogError("[SlotPanelUI] Не назначен Slot Prefab или Slots Container");
                return;
            }

            for (int i = 0; i < SlotManager.Instance.Slots.Count; i++)
            {
                var row = Instantiate(slotPrefab, slotsContainer);
                row.Bind(i, OnSlotClicked);
                _rows.Add(row);
            }

            RefreshVisuals();
        }

        /// <summary>Обновляет визуал строк без пересоздания.</summary>
        public void RefreshVisuals()
        {
            if (SlotManager.Instance == null) return;

            bool canTake = GarageManager.Instance != null && GarageManager.Instance.HasFreeBay;
            var slots = SlotManager.Instance.Slots;

            for (int i = 0; i < _rows.Count && i < slots.Count; i++)
            {
                if (_rows[i] == null) continue;
                _rows[i].Refresh(slots[i], canTake);
            }
        }

        private void OnSlotClicked(int slotIndex)
        {
            if (GarageManager.Instance == null) return;
            if (!GarageManager.Instance.HasFreeBay)
            {
                Debug.Log("[SlotPanelUI] Все посты заняты");
                return;
            }
            GarageManager.Instance.TakeCarFromSlot(slotIndex);
        }
    }
}