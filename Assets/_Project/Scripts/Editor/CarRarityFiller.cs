#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.EditorTools
{
    /// <summary>
    /// Проставляет редкость всем CarData.
    /// Запуск: Tools → Автомеханик → Расставить редкости машин.
    /// </summary>
    public static class CarRarityFiller
    {
        private static readonly (string id, CarRarity rarity)[] Data = new (string, CarRarity)[]
        {
            // ===== BASIC (3) — сразу =====
            ("car_hatch_2114", CarRarity.Basic),
            ("car_sedan_el",   CarRarity.Basic),
            ("car_golf",       CarRarity.Basic),

            // ===== MEDIUM (3) — 2 отремонтировано =====
            ("car_van",        CarRarity.Medium),
            ("car_crossover",  CarRarity.Medium),
            ("car_compact",    CarRarity.Medium),

            // ===== PREMIUM (2) — 5 отремонтировано =====
            ("car_executive",  CarRarity.Premium),
            ("car_bavaria",    CarRarity.Premium),

            // ===== LUXURY (2) — 8 отремонтировано =====
            ("car_luxury",     CarRarity.Luxury),
            ("car_sport_911",  CarRarity.Luxury),

            // ===== SECRET (1) — 10 отремонтировано =====
            ("car_secret_pickup", CarRarity.Secret),
        };

        [MenuItem("Tools/Автомеханик/Расставить редкости машин")]
        public static void FillRarities()
        {
            string[] guids = AssetDatabase.FindAssets("t:CarData");
            if (guids.Length == 0)
            {
                Debug.LogError("[CarRarityFiller] Не найдено ни одного CarData!");
                return;
            }

            int updated = 0, skipped = 0;
            var sb = new System.Text.StringBuilder("[CarRarityFiller] Результат:\n");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var car = AssetDatabase.LoadAssetAtPath<CarData>(path);
                if (car == null) continue;

                bool found = false;
                foreach (var (id, rarity) in Data)
                {
                    if (id != car.id) continue;

                    car.rarity = rarity;
                    EditorUtility.SetDirty(car);
                    sb.AppendLine($"  {car.displayName,-18} | {rarity,-8} | нужно: {CarData.GetRequiredProgress(rarity)}");
                    updated++;
                    found = true;
                    break;
                }
                if (!found)
                {
                    Debug.LogWarning($"[CarRarityFiller] Нет записи для id = {car.id}");
                    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            sb.AppendLine($"Итого обновлено: {updated}, пропущено: {skipped}");
            Debug.Log(sb.ToString());
        }
    }
}
#endif