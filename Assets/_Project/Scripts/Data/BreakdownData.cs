using UnityEngine;

namespace AutoMechanic.Data
{
    /// <summary>
    /// Визуальный эффект поломки на машине.
    /// </summary>
    public enum BreakdownVisual
    {
        Smoke,   // Дым — двигатель
        Flat,    // Спущено — колесо
        Jerk,    // Дёргается — КПП
        Rust,    // Пятна ржавчины — кузов
        Squeak,  // Визг — тормоза
        NoLight  // Не горят фары — электрика
    }

    /// <summary>
    /// Описание одной поломки.
    /// Может требовать 1 или 2 детали (ржавчина = ремкомплект + краска).
    /// Создаётся через: Assets → Create → AutoMechanic → Breakdown Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBreakdownData", menuName = "AutoMechanic/Breakdown Data")]
    public class BreakdownData : ScriptableObject
    {
        [Header("Идентификация")]
        public string id;
        public string displayName;

        [Header("Ремонт")]
        [Tooltip("Какие детали нужны. Обычно 1, для ржавчины — 2")]
        public PartData[] requiredParts;

        [Tooltip("Сколько денег дают за устранение этой поломки (20-50)")]
        [Range(0, 500)]
        public int repairReward = 20;

        [Header("Визуал")]
        public BreakdownVisual visual;

        [Tooltip("Иконка поломки для панели диагностики")]
        public Sprite icon;
    }
}