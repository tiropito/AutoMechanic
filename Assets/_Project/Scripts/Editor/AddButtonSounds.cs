#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.UI;

namespace AutoMechanic.EditorTools
{
    public static class AddButtonSounds
    {
        [MenuItem("Tools/Автомеханик/Навесить звук на все кнопки")]
        public static void AddToAll()
        {
            var buttons = Object.FindObjectsOfType<Button>(true);
            int added = 0;
            foreach (var b in buttons)
            {
                if (b.GetComponent<ButtonSound>() == null)
                {
                    Undo.AddComponent<ButtonSound>(b.gameObject);
                    added++;
                }
            }
            Debug.Log($"[AddButtonSounds] Добавлено ButtonSound: {added}");
        }
    }
}
#endif