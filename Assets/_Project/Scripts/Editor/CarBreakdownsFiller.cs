#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.EditorTools
{
    /// <summary>
    /// Раздаёт поломки машинам по тирам.
    /// Запуск: Tools → Автомеханик → Раздать поломки машинам.
    /// </summary>
    public static class CarBreakdownsFiller
    {
        // Реальные id поломок:
        // Старые (6): bd_engine, bd_wheel, bd_brakes, bd_rust, bd_transmission, bd_electric
        // Новые (7): breakdown_brake_disc, breakdown_bumper, breakdown_generator,
        //            breakdown_mirror, breakdown_oil_filter, breakdown_synchronizer, breakdown_timing_belt

        private static readonly Dictionary<string, string[]> CarBreakdowns = new Dictionary<string, string[]>
        {
            // ===== BASIC =====
            { "car_hatch_2114", new[] { "bd_engine", "bd_wheel", "breakdown_mirror" } },
            { "car_sedan_el",   new[] { "bd_engine", "bd_wheel", "breakdown_oil_filter" } },
            { "car_golf",       new[] { "bd_engine", "bd_brakes", "bd_rust" } },

            // ===== MEDIUM =====
            { "car_van",       new[] { "bd_engine", "bd_brakes", "breakdown_bumper", "breakdown_oil_filter" } },
            { "car_crossover", new[] { "bd_engine", "bd_wheel", "breakdown_brake_disc", "bd_transmission" } },
            { "car_compact",   new[] { "bd_engine", "bd_brakes", "breakdown_timing_belt", "bd_electric" } },

            // ===== PREMIUM =====
            { "car_executive", new[] { "bd_engine", "bd_transmission", "breakdown_timing_belt", "breakdown_generator" } },
            { "car_bavaria",   new[] { "bd_engine", "bd_brakes", "bd_rust", "bd_transmission" } },

            // ===== LUXURY =====
            { "car_luxury",    new[] { "bd_engine", "bd_transmission", "breakdown_generator", "breakdown_synchronizer" } },
            { "car_sport_911", new[] { "bd_engine", "breakdown_brake_disc", "breakdown_timing_belt", "breakdown_synchronizer" } },

            // ===== SECRET =====
            { "car_secret_pickup", new[] { "bd_engine", "bd_transmission", "breakdown_synchronizer", "bd_rust", "breakdown_generator" } },
        };

        [MenuItem("Tools/Автомеханик/Раздать поломки машинам")]
        public static void Fill()
        {
            var breakdownsById = new Dictionary<string, BreakdownData>();
            foreach (var g in AssetDatabase.FindAssets("t:BreakdownData"))
            {
                var bd = AssetDatabase.LoadAssetAtPath<BreakdownData>(AssetDatabase.GUIDToAssetPath(g));
                if (bd != null && !string.IsNullOrEmpty(bd.id))
                    breakdownsById[bd.id] = bd;
            }

            if (breakdownsById.Count == 0)
            {
                Debug.LogError("[CarBreakdownsFiller] Не найдено ни одного BreakdownData!");
                return;
            }

            int updated = 0, skipped = 0;
            var sb = new System.Text.StringBuilder("[CarBreakdownsFiller] Результат:\n");

            foreach (var g in AssetDatabase.FindAssets("t:CarData"))
            {
                var car = AssetDatabase.LoadAssetAtPath<CarData>(AssetDatabase.GUIDToAssetPath(g));
                if (car == null) continue;

                if (!CarBreakdowns.ContainsKey(car.id))
                {
                    Debug.LogWarning($"[CarBreakdownsFiller] Нет записи для машины: {car.id}");
                    skipped++;
                    continue;
                }

                var ids = CarBreakdowns[car.id];
                var list = new List<BreakdownData>();

                foreach (var id in ids)
                {
                    if (breakdownsById.TryGetValue(id, out var bd))
                        list.Add(bd);
                    else
                        Debug.LogWarning($"[CarBreakdownsFiller] Поломка не найдена: {id} (для {car.id})");
                }

                car.possibleBreakdowns = list.ToArray();
                EditorUtility.SetDirty(car);

                sb.Append($"[{car.rarity}] {car.displayName,-18} → ");
                sb.AppendLine(string.Join(", ", list.ConvertAll(b => b.displayName)));
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            sb.AppendLine($"\nИтого обновлено: {updated}, пропущено: {skipped}");
            Debug.Log(sb.ToString());
        }
    }
}
#endif