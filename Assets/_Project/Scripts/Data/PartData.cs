using UnityEngine;

namespace AutoMechanic.Data
{
    public enum PartCategory
    {
        Engine,
        Wheels,
        Transmission,
        Electric,
        Body
    }

    /// <summary>
    /// Редкость детали. Влияет на цену, цвет в магазине и частоту поломок.
    /// </summary>
    public enum PartRarity
    {
        Common,     // Обычная — дешёвая, частая
        Uncommon,   // Необычная
        Rare,       // Редкая
        Epic        // Эпическая — дорогая, редкая
    }

    [CreateAssetMenu(fileName = "NewPartData", menuName = "AutoMechanic/Part Data")]
    public class PartData : ScriptableObject
    {
        [Header("Идентификация")]
        public string id;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public PartCategory category;

        [Header("Редкость")]
        [Tooltip("Влияет на цену, цвет и частоту поломок")]
        public PartRarity rarity = PartRarity.Common;

        [Header("Экономика (заполняется по редкости)")]
        [Tooltip("Заполняется автоматически кнопкой Tools → Автомеханик → Проставить цены по редкости")]
        [Range(0, 10000)]
        public int buyPrice = 10;

        [Range(0, 100)]
        public int sellPercent = 60;

        [Header("Визуал")]
        public Sprite icon;

        public int SellPrice => Mathf.RoundToInt(buyPrice * sellPercent / 100f);

        /// <summary>Цвет редкости для UI.</summary>
        public Color RarityColor
        {
            get
            {
                switch (rarity)
                {
                    case PartRarity.Common:   return new Color(0.75f, 0.75f, 0.75f);
                    case PartRarity.Uncommon: return new Color(0.4f, 0.9f, 0.4f);
                    case PartRarity.Rare:     return new Color(0.4f, 0.7f, 1f);
                    case PartRarity.Epic:     return new Color(0.85f, 0.5f, 1f);
                    default:                  return Color.white;
                }
            }
        }

        public string RarityLabel
        {
            get
            {
                switch (rarity)
                {
                    case PartRarity.Common:   return "Обычная";
                    case PartRarity.Uncommon: return "Необычная";
                    case PartRarity.Rare:     return "Редкая";
                    case PartRarity.Epic:     return "Эпическая";
                    default:                  return "";
                }
            }
        }

        /// <summary>Диапазон цены для редкости (min, max) — включительно.</summary>
        public static (int min, int max) GetPriceRange(PartRarity rarity)
        {
            switch (rarity)
            {
                case PartRarity.Common:   return (10, 30);
                case PartRarity.Uncommon: return (80, 200);
                case PartRarity.Rare:     return (400, 1000);
                case PartRarity.Epic:     return (2500, 5000);
                default:                  return (10, 30);
            }
        }

        /// <summary>
        /// Детерминированная цена по id и редкости.
        /// Одна и та же деталь всегда стоит одно и то же.
        /// </summary>
        public static int ComputePrice(string id, PartRarity rarity)
        {
            var (min, max) = GetPriceRange(rarity);

            // Хэш от id — стабильный, но разный для разных деталей
            int hash = string.IsNullOrEmpty(id) ? 0 : id.GetHashCode();
            // Берём модуль, чтобы диапазон рандома был в пределах
            int seed = Mathf.Abs(hash) % 10000;
            var rng = new System.Random(seed);

            int price = rng.Next(min, max + 1);

            // Округляем до "красивого" числа
            if (price >= 1000) price = Mathf.RoundToInt(price / 100f) * 100;   // 2500, 3200
            else if (price >= 100) price = Mathf.RoundToInt(price / 10f) * 10; // 120, 180
            else price = Mathf.RoundToInt(price / 5f) * 5;                     // 15, 25

            return Mathf.Clamp(price, min, max);
        }
    }
}