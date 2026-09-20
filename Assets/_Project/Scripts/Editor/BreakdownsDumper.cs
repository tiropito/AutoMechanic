#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.EditorTools
{
    public static class BreakdownsDumper
    {
        [MenuItem("Tools/Автомеханик/Показать все id поломок")]
        public static void Dump()
        {
            var sb = new System.Text.StringBuilder("[BreakdownsDumper] Список всех BreakdownData:\n");

            foreach (var g in AssetDatabase.FindAssets("t:BreakdownData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var bd = AssetDatabase.LoadAssetAtPath<BreakdownData>(path);
                if (bd == null) continue;

                string parts = "—";
                if (bd.requiredParts != null && bd.requiredParts.Length > 0)
                {
                    var names = new System.Collections.Generic.List<string>();
                    foreach (var p in bd.requiredParts)
                        if (p != null) names.Add(p.id);
                    parts = string.Join(" + ", names);
                }

                sb.AppendLine($"  id = {bd.id,-30} | {bd.displayName,-22} | детали: {parts}");
            }

            Debug.Log(sb.ToString());
        }
    }
}
#endif