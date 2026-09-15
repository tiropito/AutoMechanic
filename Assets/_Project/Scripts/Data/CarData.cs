using UnityEngine;

namespace AutoMechanic.Data
{
    /// <summary>
    /// Тип кузова машины.
    /// </summary>
    public enum CarType
    {
        Hatchback,
        Minivan,
        Crossover,
        Sedan,
        Luxury,
        Sport
    }

    /// <summary>
    /// Данные одной машины.
    /// Создаётся через: Assets → Create → AutoMechanic → Car Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCarData", menuName = "AutoMechanic/Car Data")]
    public class CarData : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный ID, латиницей. Например: car_golf")]
        public string id;

        [Tooltip("Отображаемое имя. Например: Hatch GL")]
        public string displayName;

        [Tooltip("Реальный прототип. Например: VW Golf")]
        public string prototype;

        public CarType type;

        [Header("Особенности")]
        [Tooltip("Бонусная машина — помечается ⭐ и таймером")]
        public bool isBonus;

        [Header("Визуал")]
        [Tooltip("Спрайт машины для гаража и слотов")]
        public Sprite sprite;

        [Header("Поломки")]
        [Tooltip("Пул поломок для этой машины. Игра выберет случайно 1-3")]
        public BreakdownData[] possibleBreakdowns;
    }
}