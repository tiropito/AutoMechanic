using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.EditorTools
{
    /// <summary>
    /// Пересобирает реестры (PartDatabase, CarDatabase), не трогая сами ассеты.
    /// </summary>
    public static class DatabaseBuilder
    {
        private const string RootPath = "Assets/_Project/ScriptableObjects";
        private const string PartsPath = RootPath + "/Parts";
        private const string CarsPath = RootPath + "/Cars";

        [MenuItem("AutoMechanic/Обновить базы данных")]
        public static void RefreshDatabases()
        {
            BuildPartDatabase();
            BuildCarDatabase();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AutoMechanic] Базы данных обновлены!");
        }

        private static void BuildPartDatabase()
        {
            var path = RootPath + "/PartDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<PartDatabase>(path);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<PartDatabase>();
                AssetDatabase.CreateAsset(db, path);
            }
            var list = new List<PartData>();
            foreach (var guid in AssetDatabase.FindAssets("t:PartData", new[] { PartsPath }))
            {
                var p = AssetDatabase.LoadAssetAtPath<PartData>(AssetDatabase.GUIDToAssetPath(guid));
                if (p != null) list.Add(p);
            }
            db.allParts = list.ToArray();
            EditorUtility.SetDirty(db);
        }

        private static void BuildCarDatabase()
        {
            var path = RootPath + "/CarDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<CarDatabase>(path);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CarDatabase>();
                AssetDatabase.CreateAsset(db, path);
            }
            var list = new List<CarData>();
            foreach (var guid in AssetDatabase.FindAssets("t:CarData", new[] { CarsPath }))
            {
                var c = AssetDatabase.LoadAssetAtPath<CarData>(AssetDatabase.GUIDToAssetPath(guid));
                if (c != null) list.Add(c);
            }
            db.allCars = list.ToArray();
            EditorUtility.SetDirty(db);
        }
    }
}