using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;

namespace AutoMechanic.UI
{
    /// <summary>Панель коллекции: сетка всех машин с прогрессом.</summary>
    public class CollectionPanelUI : MonoBehaviour
    {
        public static event System.Action<bool> OnCollectionToggled;

        [Header("Ссылки")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private CollectionCardUI cardPrefab;
        [SerializeField] private Button closeButton;

        [Header("Источник данных")]
        [Tooltip("Перетащи CarDatabase.asset")]
        [SerializeField] private CarDatabase carDatabase;

        private readonly List<CollectionCardUI> _cards = new List<CollectionCardUI>();

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (rootPanel != null) rootPanel.SetActive(false);
            Subscribe();
        }

        private void OnDestroy()
        {
            if (CollectionManager.Instance != null)
                CollectionManager.Instance.OnCollectionChanged -= Refresh;
        }

        private void Subscribe()
        {
            if (CollectionManager.Instance != null)
            {
                CollectionManager.Instance.OnCollectionChanged -= Refresh;
                CollectionManager.Instance.OnCollectionChanged += Refresh;
            }
        }

        public void Open()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            if (_cards.Count == 0) Rebuild();
            Refresh();
            OnCollectionToggled?.Invoke(true);
        }

        public void Close()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            OnCollectionToggled?.Invoke(false);
        }

        public void Toggle()
        {
            if (rootPanel == null) return;
            if (rootPanel.activeSelf) Close(); else Open();
        }

        private void Rebuild()
        {
            foreach (var c in _cards) if (c != null) Destroy(c.gameObject);
            _cards.Clear();

            if (carDatabase == null || carDatabase.allCars == null)
            {
                Debug.LogError("[CollectionPanelUI] CarDatabase не назначен!");
                return;
            }
            if (cardPrefab == null || cardsContainer == null)
            {
                Debug.LogError("[CollectionPanelUI] Не назначен Card Prefab или Cards Container");
                return;
            }

            foreach (var car in carDatabase.allCars)
            {
                if (car == null) continue;
                var card = Instantiate(cardPrefab, cardsContainer);
                card.Bind(car);
                _cards.Add(card);
            }
        }

        public void Refresh()
        {
            Subscribe();

            foreach (var card in _cards)
                if (card != null) card.Refresh();

            if (progressText != null && CollectionManager.Instance != null && carDatabase != null)
            {
                int total = carDatabase.allCars != null ? carDatabase.allCars.Length : 0;
                int done = CollectionManager.Instance.RepairedCount;
                progressText.text = $"Отремонтировано: {done} / {total}";
            }
        }
    }
}