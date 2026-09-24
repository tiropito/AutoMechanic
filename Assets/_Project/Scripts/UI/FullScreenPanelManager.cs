using UnityEngine;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Гарантирует, что только одна полноэкранная панель открыта одновременно.
    /// При открытии новой — закрывает все остальные.
    /// </summary>
    public class FullScreenPanelManager : MonoBehaviour
    {
        public static FullScreenPanelManager Instance { get; private set; }

        private ShopPanelUI _shop;
        private UpgradePanelUI _upgrade;
        private CollectionPanelUI _collection;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RegisterShop(ShopPanelUI shop) => _shop = shop;
        public void RegisterUpgrade(UpgradePanelUI upg) => _upgrade = upg;
        public void RegisterCollection(CollectionPanelUI col) => _collection = col;

        /// <summary>Закрыть все панели, кроме указанной (её передай как "не трогать").</summary>
        public void CloseAllExcept(MonoBehaviour keepOpen)
        {
            if (_shop != null && _shop != keepOpen)
                _shop.Close();

            if (_upgrade != null && _upgrade != keepOpen)
                _upgrade.Close();

            if (_collection != null && _collection != keepOpen)
                _collection.Close();
        }
    }
}