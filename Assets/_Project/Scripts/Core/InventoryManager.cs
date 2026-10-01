using System;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.Core
{
    /// <summary>
    /// Инвентарь деталей. Хранится в SaveManager.Data.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [Tooltip("Реестр всех деталей. Перетащи PartDatabase.asset")]
        [SerializeField] private PartDatabase partDatabase;

        public event Action<string> OnInventoryChanged;

        public PartDatabase PartDatabase => partDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (partDatabase == null)
            {
                Debug.LogError("[InventoryManager] PartDatabase не назначен!");
            }

            SaveManager.OnDataReloaded += HandleDataReloaded;
            _ = SaveManager.Data;
        }

        private void OnDestroy()
        {
            SaveManager.OnDataReloaded -= HandleDataReloaded;
        }

        private void HandleDataReloaded()
        {
            OnInventoryChanged?.Invoke(null);
        }

        public int GetCount(string partId)
        {
            if (string.IsNullOrEmpty(partId)) return 0;
            var data = SaveManager.Data;
            int idx = data.invIds.IndexOf(partId);
            return idx >= 0 ? data.invCounts[idx] : 0;
        }

        public int GetCount(PartData part) => part == null ? 0 : GetCount(part.id);

        public bool Has(string partId, int amount = 1) => GetCount(partId) >= amount;

        public void Add(string partId, int amount = 1)
        {
            if (string.IsNullOrEmpty(partId) || amount <= 0) return;

            var data = SaveManager.Data;
            int idx = data.invIds.IndexOf(partId);

            if (idx >= 0)
                data.invCounts[idx] += amount;
            else
            {
                data.invIds.Add(partId);
                data.invCounts.Add(amount);
            }

            SaveManager.Save();
            OnInventoryChanged?.Invoke(partId);
        }

        public bool Remove(string partId, int amount = 1)
        {
            if (string.IsNullOrEmpty(partId) || amount <= 0) return false;

            var data = SaveManager.Data;
            int idx = data.invIds.IndexOf(partId);
            if (idx < 0) return false;
            if (data.invCounts[idx] < amount) return false;

            data.invCounts[idx] -= amount;
            if (data.invCounts[idx] <= 0)
            {
                data.invIds.RemoveAt(idx);
                data.invCounts.RemoveAt(idx);
            }

            SaveManager.Save();
            OnInventoryChanged?.Invoke(partId);
            return true;
        }

        public int SellOne(PartData part)
        {
            if (part == null) return 0;
            if (!Remove(part.id, 1)) return 0;

            var money = part.SellPrice;
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Add(money);
            return money;
        }

        [ContextMenu("Сбросить инвентарь")]
        public void ResetInventory()
        {
            var data = SaveManager.Data;
            data.invIds.Clear();
            data.invCounts.Clear();
            SaveManager.Save();
            OnInventoryChanged?.Invoke(null);
            Debug.Log("[InventoryManager] Инвентарь сброшен");
        }

        // ===== ТЕСТЫ =====

        [ContextMenu("ТЕСТ: +5 поршней")]
        private void TestAddPistons() => Add("part_piston", 5);

        [ContextMenu("ТЕСТ: -1 поршень")]
        private void TestRemovePiston() => Remove("part_piston", 1);

        [ContextMenu("ТЕСТ: продать 1 поршень")]
        private void TestSellPiston()
        {
            var p = partDatabase != null ? partDatabase.GetById("part_piston") : null;
            var earned = SellOne(p);
            Debug.Log(earned > 0 ? $"[InventoryManager] Продал поршень за ${earned}" : "[InventoryManager] Нет поршней");
        }

        [ContextMenu("ТЕСТ: показать всё")]
        private void TestDump()
        {
            var data = SaveManager.Data;
            var sb = new System.Text.StringBuilder("[InventoryManager]\n");
            if (data.invIds.Count == 0) sb.AppendLine("  (пусто)");
            for (int i = 0; i < data.invIds.Count; i++)
                sb.AppendLine($"  {data.invIds[i]} = {data.invCounts[i]}");
            Debug.Log(sb.ToString());
        }

        [ContextMenu("ТЕСТ: +10 всех деталей")]
        private void TestAddAllParts()
        {
            if (partDatabase == null || partDatabase.allParts == null) return;
            int added = 0;
            foreach (var part in partDatabase.allParts)
            {
                if (part == null) continue;
                Add(part.id, 10);
                added++;
            }
            Debug.Log($"[InventoryManager] Добавлено +10 к {added} видам деталей");
        }
    }
}
