#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.EditorTools
{
    /// <summary>
    /// Создаёт BreakdownData-ассеты для новых поломок.
    /// Запуск: Tools → Автомеханик → Создать новые поломки.
    /// </summary>
    public static class BreakdownsGenerator
    {
        private const string FolderPath = "Assets/_Project/Scripts/ScriptableObjects/Breakdowns";
        private const string PartsFolder = "Assets/_Project/Scripts/ScriptableObjects/Parts";

        // id, displayName, partId, visual, icon
        private struct NewBreakdown
        {
            public string id;
            public string displayName;
            public string partId;
            public BreakdownVisual visual;
        }

        private static readonly NewBreakdown[] NewBreakdowns = new NewBreakdown[]
        {
            new NewBreakdown { id = "breakdown_brake_disc",  displayName = "Износ диска",           partId = "part_brake_disc",   visual = BreakdownVisual.Squeak },
            new NewBreakdown { id = "breakdown_timing_belt", displayName = "Порван ремень ГРМ",     partId = "part_timing_belt",  visual = BreakdownVisual.Smoke },
            new NewBreakdown { id = "breakdown_oil_filter",  displayName = "Забит масляный фильтр", partId = "part_oil_filter",   visual = BreakdownVisual.Smoke },
            new NewBreakdown { id = "breakdown_synchronizer",displayName = "Хруст КПП",             partId = "part_synchronizer", visual = BreakdownVisual.Jerk },
            new NewBreakdown { id = "breakdown_generator",  displayName = "Неисправный генератор", partId = "part_generator",   visual = BreakdownVisual.NoLight },
            new NewBreakdown { id = "breakdown_mirror",      displayName = "Сломано зеркало",       partId = "part_mirror",       visual = BreakdownVisual.Rust },
            new NewBreakdown { id = "breakdown_bumper",      displayName = "Повреждён бампер",      partId = "part_bumper",       visual = BreakdownVisual.Rust },
        };

        [MenuItem("Tools/Автомеханик/Создать новые поломки")]
        public static void Generate()
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
                AssetDatabase.Refresh();
            }

            int created = 0, skipped = 0;

            foreach (var nb in NewBreakdowns)
            {
                string assetPath = $"{FolderPath}/{nb.id}.asset";

                if (File.Exists(assetPath))
                {
                    Debug.Log($"[BreakdownsGenerator] Уже существует: {nb.id}");
                    skipped++;
                    continue;
                }

                var bd = ScriptableObject.CreateInstance<BreakdownData>();
                bd.id = nb.id;
                bd.displayName = nb.displayName;
                bd.visual = nb.visual;

                // Ищем нужную деталь
                var part = FindPart(nb.partId);
                if (part == null)
                {
                    Debug.LogWarning($"[BreakdownsGenerator] Деталь не найдена: {nb.partId}");
                    continue;
                }

                bd.requiredParts = new PartData[] { part };
                bd.repairReward = 50; // fallback, не критично — награда считается динамически

                AssetDatabase.CreateAsset(bd, assetPath);
                created++;

                Debug.Log($"[BreakdownsGenerator] ✓ Создано: {nb.displayName} ({nb.id}) — требуется {part.displayName}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BreakdownsGenerator] Итого: создано {created}, пропущено {skipped}");
        }

        private static PartData FindPart(string id)
        {
            string[] guids = AssetDatabase.FindAssets("t:PartData");
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var part = AssetDatabase.LoadAssetAtPath<PartData>(path);
                if (part != null && part.id == id) return part;
            }
            return null;
        }
    }
}
#endif