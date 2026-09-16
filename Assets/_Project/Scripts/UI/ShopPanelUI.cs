using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Панель магазина. Открывается кнопкой «Магазин», закрывается кнопкой «Закрыть».
    /// </summary>
    public class ShopPanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TMP_Text moneyText;
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

            // Каждый раз пробуем получить базу — на случай, если InventoryManager создался позже
            TryFetchDatabase();

            if (!_built) { Rebuild(); _built = true; }
            RefreshAll();
        }

        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
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
            if (_database == null)
                Debug.LogError("[ShopPanelUI] PartDatabase не назначен в InventoryManager!");
            else
                Debug.Log($"[ShopPanelUI] База найдена, деталей: {(_database.allParts != null ? _database.allParts.Length : 0)}");
        }

        private void Rebuild()
        {
            foreach (var it in _items) if (it != null) Destroy(it.gameObject);
            _items.Clear();

            if (_database == null || _database.allParts == null)
            {
                Debug.LogError("[ShopPanelUI] Нет базы — не могу построить магазин");
                _built = false; // попробуем ещё раз при следующем Open
                return;
            }
            if (itemPrefab == null)
            {
                Debug.LogError("[ShopPanelUI] Item Prefab не назначен!");
                _built = false;
                return;
            }
            if (itemsContainer == null)
            {
                Debug.LogError("[ShopPanelUI] Items Container не назначен!");
                _built = false;
                return;
            }

            int count = 0;
            foreach (var part in _database.allParts)
            {
                if (part == null) continue;
                var row = Instantiate(itemPrefab, itemsContainer);
                row.Bind(part, OnBuyClicked);
                _items.Add(row);
                count++;
            }
            Debug.Log($"[ShopPanelUI] Построено строк: {count}");
        }

        private void OnBuyClicked(PartData part)
        {
            if (ShopManager.Instance == null) return;
            ShopManager.Instance.TryBuy(part, 1);
            RefreshAll();
        }

        public void RefreshAll()
        {
            if (moneyText != null && ShopManager.Instance != null)
                moneyText.text = $"${ShopManager.Instance.GetCurrentMoney()}";

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