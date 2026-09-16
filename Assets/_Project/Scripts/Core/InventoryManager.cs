using System;
using System.Collections.Generic;
using UnityEngine;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;   // для DiagnosticManager / GarageManager в тестовом методе

namespace AutoMechanic.Core
{
    /// <summary>
    /// Инвентарь игрока: сколько каких деталей лежит.
    /// Сохраняется в PlayerPrefs как набор пар "part_id" -> quantity.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        private const string SaveKeyPrefix = "am_inv_";

        [Tooltip("Реестр всех деталей. Перетащи сюда PartDatabase.asset")]
        [SerializeField] private PartDatabase partDatabase;

        // part_id -> количество
        private readonly Dictionary<string, int> _items = new Dictionary<string, int>();

        /// <summary>Вызывается при изменении любой ячейки. Аргумент — ID детали.</summary>
        public event Action<string> OnInventoryChanged;

        /// <summary>Реестр деталей (для тестовых методов и UI)</summary>
        public PartDatabase PartDatabase => partDatabase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (partDatabase == null)
            {
                Debug.LogError("[InventoryManager] PartDatabase не назначен в Inspector!");
                return;
            }

            Load();
        }

        /// <summary>Сколько деталей с данным ID у игрока</summary>
        public int GetCount(string partId)
        {
            return _items.TryGetValue(partId, out var count) ? count : 0;
        }

        /// <summary>Удобный вариант через PartData</summary>
        public int GetCount(PartData part) => part == null ? 0 : GetCount(part.id);

        /// <summary>Есть ли хотя бы одна деталь</summary>
        public bool Has(string partId, int amount = 1) => GetCount(partId) >= amount;

        /// <summary>Добавить детали (покупка, награда)</summary>
        public void Add(string partId, int amount = 1)
        {
            if (string.IsNullOrEmpty(partId) || amount <= 0) return;
            _items[partId] = GetCount(partId) + amount;
            Save(partId);
            OnInventoryChanged?.Invoke(partId);
        }

        /// <summary>Убрать детали. Возвращает false, если не хватает.</summary>
        public bool Remove(string partId, int amount = 1)
        {
            if (string.IsNullOrEmpty(partId) || amount <= 0) return false;
            var current = GetCount(partId);
            if (current < amount) return false;

            _items[partId] = current - amount;
            Save(partId);
            OnInventoryChanged?.Invoke(partId);
            return true;
        }

        /// <summary>Продать одну деталь. Возвращает деньги при успехе, 0 — при провале.</summary>
        public int SellOne(PartData part)
        {
            if (part == null) return 0;
            if (!Remove(part.id, 1)) return 0;

            var money = part.SellPrice;
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Add(money);
            return money;
        }

        /// <summary>Полный сброс инвентаря (для отладки)</summary>
        [ContextMenu("Сбросить инвентарь")]
        public void ResetInventory()
        {
            foreach (var key in new List<string>(_items.Keys))
            {
                PlayerPrefs.DeleteKey(SaveKeyPrefix + key);
            }
            _items.Clear();
            OnInventoryChanged?.Invoke(null);
            Debug.Log("[InventoryManager] Инвентарь сброшен");
        }

        private void Save(string partId)
        {
            PlayerPrefs.SetInt(SaveKeyPrefix + partId, GetCount(partId));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            _items.Clear();
            if (partDatabase.allParts == null) return;

            foreach (var part in partDatabase.allParts)
            {
                if (part == null) continue;
                var count = PlayerPrefs.GetInt(SaveKeyPrefix + part.id, 0);
                if (count > 0) _items[part.id] = count;
            }
            Debug.Log($"[InventoryManager] Загружено видов деталей: {_items.Count}");
        }

        // ==================== ТЕСТЫ (ПКМ по компоненту) ====================

        [ContextMenu("ТЕСТ: +5 поршней")]
        private void TestAddPistons() => Add("part_piston", 5);

        [ContextMenu("ТЕСТ: -1 поршень")]
        private void TestRemovePiston() => Remove("part_piston", 1);

        [ContextMenu("ТЕСТ: продать 1 поршень")]
        private void TestSellPiston()
        {
            var p = partDatabase.GetById("part_piston");
            var earned = SellOne(p);
            Debug.Log(earned > 0 ? $"[InventoryManager] Продал поршень за ${earned}" : "[InventoryManager] Нет поршней");
        }

        [ContextMenu("ТЕСТ: показать всё")]
        private void TestDump()
        {
            var sb = new System.Text.StringBuilder("[InventoryManager]\n");
            if (_items.Count == 0) sb.AppendLine("  (пусто)");
            foreach (var kv in _items)
                sb.AppendLine($"  {kv.Key} = {kv.Value}");
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Разом добавить +10 каждой детали из PartDatabase.
        /// Самый быстрый способ наполнить инвентарь для тестов.
        /// </summary>
        [ContextMenu("ТЕСТ: +10 всех деталей")]
        private void TestAddAllParts()
        {
            if (partDatabase == null || partDatabase.allParts == null)
            {
                Debug.LogError("[InventoryManager] PartDatabase пустой!");
                return;
            }

            int added = 0;
            foreach (var part in partDatabase.allParts)
            {
                if (part == null) continue;
                Add(part.id, 10);
                added++;
            }
            Debug.Log($"[InventoryManager] Добавлено +10 к {added} видам деталей");
        }

        /// <summary>
        /// Добавить ровно те детали, которые нужны для поломок в сессии 0 гаража.
        /// Удобно для теста связки «диагностика → ремонт».
        /// </summary>
        [ContextMenu("ТЕСТ: дать детали под сессию 0")]
        private void TestGrantPartsForSession0()
        {
            if (DiagnosticManager.Instance == null)
            {
                Debug.LogError("[InventoryManager] DiagnosticManager не найден на сцене");
                return;
            }

            var breakdowns = DiagnosticManager.Instance.GetBreakdowns(0);
            if (breakdowns == null || breakdowns.Count == 0)
            {
                Debug.LogWarning("[InventoryManager] Сессия 0 пуста. Сначала диагностика!");
                return;
            }

            int added = 0;
            var sb = new System.Text.StringBuilder("[InventoryManager] Выданы детали под сессию 0:\n");

            foreach (var bd in breakdowns)
            {
                if (bd == null || bd.requiredParts == null) continue;

                foreach (var part in bd.requiredParts)
                {
                    if (part == null) continue;
                    Add(part.id, 1);
                    sb.AppendLine($"  + 1 {part.displayName} (для «{bd.displayName}»)");
                    added++;
                }
            }

            sb.AppendLine($"Итого выдано: {added}");
            Debug.Log(sb.ToString());
        }
    }
}