using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.EditorTools
{
    /// <summary>
    /// Генератор всех игровых данных из ТЗ.
    /// Запуск: меню AutoMechanic → Сгенерировать все данные.
    /// </summary>
    public static class DataGenerator
    {
        private const string PartsPath = "Assets/_Project/ScriptableObjects/Parts";
        private const string BreakdownsPath = "Assets/_Project/ScriptableObjects/Breakdowns";
        private const string CarsPath = "Assets/_Project/ScriptableObjects/Cars";

        [MenuItem("AutoMechanic/Сгенерировать все данные")]
        public static void GenerateAll()
        {
            EnsureFolder(PartsPath);
            EnsureFolder(BreakdownsPath);
            EnsureFolder(CarsPath);

            GenerateParts();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GenerateBreakdowns();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GenerateCars();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AutoMechanic] Все данные сгенерированы!");
        }

        // ==================== ДЕТАЛИ (18) ====================

        private static void GenerateParts()
        {
            // Двигатель
            CreatePart("part_piston",       "Поршень",              PartCategory.Engine,       25);
            CreatePart("part_spark_plug",   "Свеча",                PartCategory.Engine,       10);
            CreatePart("part_timing_belt",  "Ремень ГРМ",           PartCategory.Engine,       20);
            CreatePart("part_oil_filter",   "Масляный фильтр",      PartCategory.Engine,       10);
            // Колёса
            CreatePart("part_tire",         "Шина",                 PartCategory.Wheels,       30);
            CreatePart("part_rim",          "Диск",                 PartCategory.Wheels,       25);
            CreatePart("part_brake_disc",   "Тормозной диск",       PartCategory.Wheels,       20);
            CreatePart("part_brake_pad",    "Тормозная колодка",    PartCategory.Wheels,       15);
            // КПП
            CreatePart("part_clutch",       "Сцепление",            PartCategory.Transmission, 30);
            CreatePart("part_gear",         "Шестерня",             PartCategory.Transmission, 25);
            CreatePart("part_synchronizer", "Синхронизатор",        PartCategory.Transmission, 20);
            // Электрика
            CreatePart("part_battery",      "Аккумулятор",          PartCategory.Electric,     30);
            CreatePart("part_generator",    "Генератор",            PartCategory.Electric,     25);
            CreatePart("part_headlight",    "Фара",                 PartCategory.Electric,     15);
            // Кузов
            CreatePart("part_rust_kit",     "Ремкомплект ржавчины", PartCategory.Body,         20);
            CreatePart("part_paint",        "Краска",               PartCategory.Body,         15);
            CreatePart("part_bumper",       "Бампер",               PartCategory.Body,         25);
            CreatePart("part_mirror",       "Зеркало",              PartCategory.Body,         10);
        }

        // ==================== ПОЛОМКИ (6) ====================

        private static void GenerateBreakdowns()
        {
            CreateBreakdown("bd_engine",       "Двигатель", BreakdownVisual.Smoke,  30, "part_piston");
            CreateBreakdown("bd_wheel",        "Колесо",    BreakdownVisual.Flat,   25, "part_tire");
            CreateBreakdown("bd_transmission", "КПП",       BreakdownVisual.Jerk,   40, "part_clutch");
            CreateBreakdown("bd_rust",         "Ржавчина",  BreakdownVisual.Rust,   50, "part_rust_kit", "part_paint");
            CreateBreakdown("bd_brakes",       "Тормоза",   BreakdownVisual.Squeak, 30, "part_brake_pad");
            CreateBreakdown("bd_electric",     "Электрика", BreakdownVisual.NoLight,35, "part_battery");
        }

        // ==================== МАШИНЫ (10) ====================

        private static void GenerateCars()
        {
            CreateCar("car_golf",       "Hatch GL",       "VW Golf",              CarType.Hatchback, false);
            CreateCar("car_van",        "Van LT",         "Citroen SpaceTourer",  CarType.Minivan,   false);
            CreateCar("car_crossover",  "Crossover Q6",   "Audi Q6",              CarType.Crossover, false);
            CreateCar("car_executive",  "Executive E",    "Mercedes E-Class",     CarType.Sedan,     false);
            CreateCar("car_compact",    "Compact C",      "Mercedes C-Class",     CarType.Sedan,     false);
            CreateCar("car_luxury",     "Luxury Limo",    "Maybach",              CarType.Luxury,    true);
            CreateCar("car_sedan_el",   "Sedan EL",       "Hyundai Elantra",      CarType.Sedan,     false);
            CreateCar("car_hatch_2114", "Hatch 2114",     "ВАЗ 2114",             CarType.Hatchback, false);
            CreateCar("car_bavaria",    "Bavaria E30",    "BMW E30",              CarType.Sedan,     false);
            CreateCar("car_sport_911",  "Sport 911",      "Porsche 911 Classic",  CarType.Sport,     true);
        }

        // ==================== ХЕЛПЕРЫ ====================

        private static void CreatePart(string id, string name, PartCategory cat, int price)
        {
            var asset = ScriptableObject.CreateInstance<PartData>();
            asset.id = id;
            asset.displayName = name;
            asset.category = cat;
            asset.buyPrice = price;
            asset.sellPercent = 60;
            SaveAsset(asset, $"{PartsPath}/{id}.asset");
        }

        private static void CreateBreakdown(string id, string name, BreakdownVisual vis, int reward, params string[] partIds)
        {
            var asset = ScriptableObject.CreateInstance<BreakdownData>();
            asset.id = id;
            asset.displayName = name;
            asset.visual = vis;
            asset.repairReward = reward;

            var parts = new List<PartData>();
            foreach (var pid in partIds)
            {
                var part = AssetDatabase.LoadAssetAtPath<PartData>($"{PartsPath}/{pid}.asset");
                if (part != null) parts.Add(part);
                else Debug.LogWarning($"[AutoMechanic] Не найдена деталь: {pid}");
            }
            asset.requiredParts = parts.ToArray();
            SaveAsset(asset, $"{BreakdownsPath}/{id}.asset");
        }

        private static void CreateCar(string id, string name, string proto, CarType type, bool bonus)
        {
            var asset = ScriptableObject.CreateInstance<CarData>();
            asset.id = id;
            asset.displayName = name;
            asset.prototype = proto;
            asset.type = type;
            asset.isBonus = bonus;

            // Пока все поломки в пул — потом сузим по машинам
            var bds = new List<BreakdownData>();
            foreach (var guid in AssetDatabase.FindAssets("t:BreakdownData", new[] { BreakdownsPath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var bd = AssetDatabase.LoadAssetAtPath<BreakdownData>(path);
                if (bd != null) bds.Add(bd);
            }
            asset.possibleBreakdowns = bds.ToArray();
            SaveAsset(asset, $"{CarsPath}/{id}.asset");
        }

        private static void SaveAsset(ScriptableObject asset, string path)
        {
            if (File.Exists(path))
            {
                Debug.Log($"[AutoMechanic] Уже есть, пропускаю: {path}");
                return;
            }
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace("\\", "/");
            var name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}