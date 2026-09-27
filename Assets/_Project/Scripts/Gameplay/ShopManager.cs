using UnityEngine;
using AutoMechanic.Core;
using AutoMechanic.Data;

namespace AutoMechanic.Gameplay
{
    /// <summary>
    /// Магазин: покупка деталей за деньги.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Купить N деталей. Возвращает true при успехе.</summary>
        public bool TryBuy(PartData part, int amount = 1)
        {
            if (part == null || amount <= 0) return false;

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[ShopManager] Нет InventoryManager");
                return false;
            }
            if (EconomyManager.Instance == null)
            {
                Debug.LogError("[ShopManager] Нет EconomyManager");
                return false;
            }

            int totalCost = part.buyPrice * amount;
            if (!EconomyManager.Instance.Spend(totalCost))
            {
                Debug.Log($"[ShopManager] Не хватает денег: нужно ${totalCost}, есть ${EconomyManager.Instance.Money}");
                return false;
            }

            InventoryManager.Instance.Add(part.id, amount);
            Debug.Log($"[ShopManager] Куплено: {part.displayName} ×{amount} за ${totalCost}");
            return true;
        }

        /// <summary>Можно ли купить N деталей прямо сейчас.</summary>
        public bool CanAfford(PartData part, int amount, out int totalCost, out int currentMoney)
        {
            totalCost = part != null ? part.buyPrice * amount : 0;
            currentMoney = EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0;
            return currentMoney >= totalCost;
        }

        /// <summary>Текущий баланс игрока.</summary>
        public int GetCurrentMoney()
        {
            return EconomyManager.Instance != null ? EconomyManager.Instance.Money : 0;
        }

        /// <summary>Цена покупки детали.</summary>
        public int GetBuyPrice(PartData part)
        {
            return part != null ? part.buyPrice : 0;
        }

        // ============ ТЕСТЫ ============

        [ContextMenu("ТЕСТ: показать баланс")]
        private void TestMoney()
        {
            Debug.Log($"[ShopManager] Баланс: ${GetCurrentMoney()}");
        }

        [ContextMenu("ТЕСТ: купить поршень")]
        private void TestBuyPiston()
        {
            if (InventoryManager.Instance == null || InventoryManager.Instance.PartDatabase == null)
            {
                Debug.LogError("[ShopManager] Нет InventoryManager/PartDatabase");
                return;
            }
            var p = InventoryManager.Instance.PartDatabase.GetById("part_piston");
            if (p == null)
            {
                Debug.LogError("[ShopManager] Деталь 'part_piston' не найдена");
                return;
            }
            TryBuy(p, 1);
        }
    }
}