using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    public class ShopPanelUI : MonoBehaviour
    {
        public static event Action<bool> OnShopToggled;

        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private ShopItemUI itemPrefab;
        [SerializeField] private Button closeButton;

        private readonly List<ShopItemUI> _items = new List<ShopItemUI>();
        private PartDatabase _database;
        private bool _built;

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        public void Open()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            TryFetchDatabase();
            if (!_built) { Rebuild(); _built = true; }
            RefreshAll();
            OnShopToggled?.Invoke(true);
        }

        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            OnShopToggled?.Invoke(false);
        }

        public void Toggle()
        {
            if (rootPanel == null) return;
            if (rootPanel.activeSelf) Close(); else Open();
        }

        private void TryFetchDatabase()
        {
            if (_database != null) return;
            if (InventoryManager.Instance == null) return;
            _database = InventoryManager.Instance.PartDatabase;
        }

        private void Rebuild()
        {
            foreach (var it in _items) if (it != null) Destroy(it.gameObject);
            _items.Clear();

            if (_database == null || _database.allParts == null)
            {
                Debug.LogError("[ShopPanelUI] Нет базы");
                _built = false;
                return;
            }
            if (itemPrefab == null || itemsContainer == null)
            {
                Debug.LogError("[ShopPanelUI] Не назначен префаб или контейнер");
                _built = false;
                return;
            }

            foreach (var part in _database.allParts)
            {
                if (part == null) continue;
                var row = Instantiate(itemPrefab, itemsContainer);
                row.Bind(part, OnBuyClicked, OnSellClicked);
                _items.Add(row);
            }
        }

        private void OnBuyClicked(PartData part)
        {
            if (ShopManager.Instance == null) return;
            ShopManager.Instance.TryBuy(part, 1);
            RefreshAll();
        }

        private void OnSellClicked(PartData part)
        {
            if (InventoryManager.Instance == null) return;
            int earned = InventoryManager.Instance.SellOne(part);
            if (earned > 0)
                Debug.Log($"[ShopPanelUI] Продано: {part.displayName} за ${earned}");
            RefreshAll();
        }

        public void RefreshAll()
        {

            foreach (var it in _items)
            {
                if (it == null || it.Part == null) continue;
                int have = InventoryManager.Instance != null
                    ? InventoryManager.Instance.GetCount(it.Part.id) : 0;
                bool can = ShopManager.Instance != null &&
                           ShopManager.Instance.CanAfford(it.Part, 1, out _, out _);
                it.RefreshVisual(have, can);
            }
        }
    }
}