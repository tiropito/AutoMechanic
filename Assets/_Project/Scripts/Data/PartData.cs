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

    public enum PartRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic
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
        public PartRarity rarity = PartRarity.Common;

        [Header("Экономика")]
        [Range(0, 10000)]
        public int buyPrice = 10;

        [Range(0, 100)]
        public int sellPercent = 60;

        [Header("Визуал")]
        public Sprite icon;

        public int SellPrice => Mathf.RoundToInt(buyPrice * sellPercent / 100f);

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

        public static int ComputePrice(string id, PartRarity rarity)
        {
            var (min, max) = GetPriceRange(rarity);
            int hash = string.IsNullOrEmpty(id) ? 0 : id.GetHashCode();
            int seed = Mathf.Abs(hash) % 10000;
            var rng = new System.Random(seed);

            int price = rng.Next(min, max + 1);

            if (price >= 1000) price = Mathf.RoundToInt(price / 100f) * 100;
            else if (price >= 100) price = Mathf.RoundToInt(price / 10f) * 10;
            else price = Mathf.RoundToInt(price / 5f) * 5;

            return Mathf.Clamp(price, min, max);
        }
    }
}