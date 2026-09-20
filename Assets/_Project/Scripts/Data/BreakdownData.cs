using UnityEngine;

namespace AutoMechanic.Data
{
    public enum BreakdownVisual
    {
        Smoke,
        Flat,
        Jerk,
        Rust,
        Squeak,
        NoLight
    }

    [CreateAssetMenu(fileName = "NewBreakdownData", menuName = "AutoMechanic/Breakdown Data")]
    public class BreakdownData : ScriptableObject
    {
        [Header("Идентификация")]
        public string id;
        public string displayName;

        [Header("Ремонт")]
        public PartData[] requiredParts;

        [Tooltip("Сколько денег дают за устранение этой поломки (fallback, если нет авто-расчёта)")]
        [Range(0, 20000)]
        public int repairReward = 20;

        [Header("Визуал")]
        public BreakdownVisual visual;
        public Sprite icon;

        /// <summary>Редкость поломки = самая редкая деталь из requiredParts.</summary>
        public PartRarity GetRarity()
        {
            if (requiredParts == null || requiredParts.Length == 0)
                return PartRarity.Common;

            PartRarity max = PartRarity.Common;
            foreach (var p in requiredParts)
                if (p != null && p.rarity > max) max = p.rarity;
            return max;
        }

        /// <summary>Суммарная стоимость деталей для этой поломки.</summary>
        public int GetPartsCost()
        {
            if (requiredParts == null) return 0;
            int total = 0;
            foreach (var p in requiredParts)
                if (p != null) total += p.buyPrice;
            return total;
        }
    }
}