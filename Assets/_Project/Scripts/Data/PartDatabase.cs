using System.Collections.Generic;
using UnityEngine;

namespace AutoMechanic.Data
{
    /// <summary>
    /// Реестр всех деталей игры. Один ассет, внутри — массив.
    /// Нужен, чтобы по ID из PlayerPrefs найти PartData.
    /// </summary>
    [CreateAssetMenu(fileName = "PartDatabase", menuName = "AutoMechanic/Part Database")]
    public class PartDatabase : ScriptableObject
    {
        [Tooltip("Все детали игры. Заполняется автоматически генератором.")]
        public PartData[] allParts;

        private Dictionary<string, PartData> _lookup;

        /// <summary>Найти деталь по ID. Кэширует словарь при первом вызове.</summary>
        public PartData GetById(string id)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, PartData>();
                if (allParts != null)
                {
                    foreach (var p in allParts)
                    {
                        if (p != null && !string.IsNullOrEmpty(p.id))
                            _lookup[p.id] = p;
                    }
                }
            }
            return _lookup.TryGetValue(id, out var result) ? result : null;
        }
    }
}