#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.EditorTools
{
    public static class PartsRarityFiller
    {
        private static readonly (string id, PartRarity rarity)[] Data = new (string, PartRarity)[]
        {
            ("part_spark_plug",   PartRarity.Common),
            ("part_oil_filter",   PartRarity.Common),
            ("part_tire",         PartRarity.Common),
            ("part_brake_pad",    PartRarity.Common),
            ("part_headlight",    PartRarity.Common),
            ("part_paint",        PartRarity.Common),
            ("part_mirror",       PartRarity.Common),
            ("part_rim",          PartRarity.Uncommon),
            ("part_brake_disc",   PartRarity.Uncommon),
            ("part_gear",         PartRarity.Uncommon),
            ("part_battery",      PartRarity.Uncommon),
            ("part_bumper",       PartRarity.Uncommon),
            ("part_timing_belt",  PartRarity.Rare),
            ("part_clutch",       PartRarity.Rare),
            ("part_alternator",   PartRarity.Rare),
            ("part_rust_kit",     PartRarity.Rare),
            ("part_piston",       PartRarity.Epic),
            ("part_synchronizer", PartRarity.Epic),
        };

        [MenuItem("Tools/Автомеханик/Расставить редкости и цены")]
        public static void FillRarities()
        {
            string[] guids = AssetDatabase.FindAssets("t:PartData");
            if (guids.Length == 0)
            {
                Debug.LogError("[PartsRarityFiller] Не найдено ни одного PartData!");
                return;
            }

            int updated = 0, skipped = 0;
            var sb = new System.Text.StringBuilder("[PartsRarityFiller] Результат:\n");

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var part = AssetDatabase.LoadAssetAtPath<PartData>(path);
                if (part == null) continue;

                bool found = false;
                foreach (var (id, rarity) in Data)
                {
                    if (id != part.id) continue;
                    part.rarity = rarity;
                    part.buyPrice = PartData.ComputePrice(part.id, rarity);
                    EditorUtility.SetDirty(part);
                    sb.AppendLine($"  {part.displayName,-22} | {rarity,-9} | ${part.buyPrice}");
                    updated++;
                    found = true;
                    break;
                }
                if (!found)
                {
                    Debug.LogWarning($"[PartsRarityFiller] Нет записи для id = {part.id}");
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