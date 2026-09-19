using UnityEngine;

namespace AutoMechanic.Data
{
    /// <summary>
    /// Категория детали — для фильтрации в магазине и UI.
    /// </summary>
    public enum PartCategory
    {
        Engine,        // Двигатель
        Wheels,        // Колёса
        Transmission,  // КПП
        Electric,      // Электрика
        Body           // Кузов
    }

    /// <summary>
    /// Данные одной детали. ScriptableObject — чтобы не хардкодить в коде.
    /// Создаётся через: Assets → Create → AutoMechanic → Part Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPartData", menuName = "AutoMechanic/Part Data")]
    public class PartData : ScriptableObject
    {
        [Header("Идентификация")]
        [Tooltip("Уникальный ID, латиницей, snake_case. Например: part_piston")]
        public string id;

        [Tooltip("Отображаемое имя для UI. Например: Поршень")]
        public string displayName;

        [Tooltip("Короткое описание: что это за деталь и зачем нужна")]
        [TextArea(2, 4)]
        public string description;

        [Tooltip("Категория детали")]
        public PartCategory category;

        [Header("Экономика")]
        [Tooltip("Цена покупки в магазине (10-30)")]
        [Range(0, 1000)]
        public int buyPrice = 10;

        [Tooltip("Процент от цены покупки, за который можно продать (50-70%)")]
        [Range(0, 100)]
        public int sellPercent = 60;

        [Header("Визуал")]
        [Tooltip("Иконка детали для UI")]
        public Sprite icon;

        /// <summary>Сколько денег дадут при продаже</summary>
        public int SellPrice => Mathf.RoundToInt(buyPrice * sellPercent / 100f);
    }
}