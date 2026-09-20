using UnityEngine;

namespace AutoMechanic.Data
{
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
    /// Редкость машины. Влияет на то, когда она открывается.
    /// </summary>
    public enum CarRarity
    {
        Basic,      // Открыта сразу
        Medium,     // 2 уникальные машины отремонтированы
        Premium,    // 5 уникальных
        Luxury,     // 8 уникальных
        Secret      // 10 уникальных (все)
    }

    [CreateAssetMenu(fileName = "NewCarData", menuName = "AutoMechanic/Car Data")]
    public class CarData : ScriptableObject
    {
        [Header("Идентификация")]
        public string id;
        public string displayName;
        public string prototype;
        public CarType type;

        [Header("Редкость")]
        [Tooltip("Определяет, с какого прогресса машина открывается")]
        public CarRarity rarity = CarRarity.Basic;

        [Header("Особенности")]
        public bool isBonus;

        [Header("Визуал")]
        public Sprite sprite;

        [Header("Поломки")]
        public BreakdownData[] possibleBreakdowns;

        /// <summary>Цвет редкости для UI.</summary>
        public Color RarityColor
        {
            get
            {
                switch (rarity)
                {
                    case CarRarity.Basic:   return new Color(0.85f, 0.85f, 0.85f);
                    case CarRarity.Medium:  return new Color(0.4f, 0.9f, 0.4f);
                    case CarRarity.Premium: return new Color(0.4f, 0.7f, 1f);
                    case CarRarity.Luxury:  return new Color(1f, 0.8f, 0.3f);
                    case CarRarity.Secret:  return new Color(0.85f, 0.5f, 1f);
                    default:                return Color.white;
                }
            }
        }

        public string RarityLabel
        {
            get
            {
                switch (rarity)
                {
                    case CarRarity.Basic:   return "Базовая";
                    case CarRarity.Medium:  return "Средняя";
                    case CarRarity.Premium: return "Премиум";
                    case CarRarity.Luxury:  return "Люкс";
                    case CarRarity.Secret:  return "Секретная";
                    default:                return "";
                }
            }
        }

        /// <summary>Сколько уникальных машин нужно отремонтировать для открытия этого тира.</summary>
        public static int GetRequiredProgress(CarRarity rarity)
        {
            switch (rarity)
            {
                case CarRarity.Basic:   return 0;
                case CarRarity.Medium:  return 2;
                case CarRarity.Premium: return 5;
                case CarRarity.Luxury:  return 8;
                case CarRarity.Secret:  return 10;
                default:                return 0;
            }
        }
    }
}