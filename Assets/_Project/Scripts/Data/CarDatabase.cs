using System.Collections.Generic;
using UnityEngine;

namespace AutoMechanic.Data
{
    /// <summary>
    /// Реестр всех машин игры. Один ассет, внутри — массив.
    /// </summary>
    [CreateAssetMenu(fileName = "CarDatabase", menuName = "AutoMechanic/Car Database")]
    public class CarDatabase : ScriptableObject
    {
        [Tooltip("Все машины игры. Заполняется автоматически генератором.")]
        public CarData[] allCars;

        private Dictionary<string, CarData> _lookup;

        public CarData GetById(string id)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, CarData>();
                if (allCars != null)
                {
                    foreach (var c in allCars)
                        if (c != null && !string.IsNullOrEmpty(c.id))
                            _lookup[c.id] = c;
                }
            }
            return _lookup.TryGetValue(id, out var result) ? result : null;
        }
    }
}