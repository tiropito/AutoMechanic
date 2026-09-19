#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AutoMechanic.Data;

namespace AutoMechanic.EditorTools
{
    /// <summary>
    /// Разовый скрипт: заполняет поле description у всех PartData в папке Parts.
    /// Запуск: Tools → Автомеханик → Заполнить описания деталей.
    /// </summary>
    public static class PartDescriptionsFiller
    {
        // Таблица: id → описание
        private static readonly (string id, string desc)[] Descriptions = new (string, string)[]
        {
            // Двигатель
            ("part_piston",      "Сгорает топливо, толкает коленвал. Сердце двигателя."),
            ("part_spark_plug",  "Поджигает топливо в цилиндре. Без неё мотор не заведётся."),
            ("part_timing_belt", "Синхронизирует клапаны и поршни. Разрыв = капремонт."),
            ("part_oil_filter",  "Очищает масло от грязи и металлической стружки."),

            // Колёса
            ("part_tire",        "Резина колеса. Отвечает за сцепление с дорогой."),
            ("part_rim",         "Металлическая основа колеса. Крепится к ступице."),
            ("part_brake_disc",  "Вращается с колесом. Колодки зажимают его при торможении."),
            ("part_brake_pad",   "Прижимается к диску, останавливая машину."),

            // КПП
            ("part_clutch",       "Соединяет двигатель с коробкой. Плавный старт с места."),
            ("part_gear",         "Зубчатое колесо в КПП. Передаёт крутящий момент."),
            ("part_synchronizer", "Выравнивает скорость шестерён при переключении."),

            // Электрика
            ("part_battery",    "Питает стартер и электрику. Запускает двигатель."),
            ("part_alternator", "Заряжает аккумулятор во время движения."),
            ("part_headlight",  "Освещает дорогу ночью. Ездить без неё нельзя."),

            // Кузов
            ("part_rust_kit", "Удаляет ржавчину с кузова. Нужен вместе с краской."),
            ("part_paint",    "Защитное и декоративное покрытие кузова."),
            ("part_bumper",   "Защищает перед и зад машины при ударах."),
            ("part_mirror",   "Боковое зеркало. Помогает видеть, что сзади.")
        };

        [MenuItem("Tools/Автомеханик/Заполнить описания деталей")]
        public static void FillDescriptions()
        {
            string[] guids = AssetDatabase.FindAssets("t:PartData");
            if (guids.Length == 0)
            {
                Debug.LogError("[PartDescriptionsFiller] Не найдено ни одного PartData!");
                return;
            }

            int updated = 0;
            int skipped = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var part = AssetDatabase.LoadAssetAtPath<PartData>(path);
                if (part == null) continue;

                string desc = FindDescription(part.id);
                if (string.IsNullOrEmpty(desc))
                {
                    Debug.LogWarning($"[PartDescriptionsFiller] Нет описания для id = {part.id} ({part.name})");
                    skipped++;
                    continue;
                }

                part.description = desc;
                EditorUtility.SetDirty(part);
                updated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PartDescriptionsFiller] Обновлено: {updated}, пропущено: {skipped}");
        }

        private static string FindDescription(string id)
        {
            foreach (var (key, desc) in Descriptions)
                if (key == id) return desc;
            return null;
        }
    }
}
#endif