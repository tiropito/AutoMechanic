using System;
using UnityEngine;
using YG;

namespace AutoMechanic.Core
{
    /// <summary>
    /// Центральный менеджер сохранений.
    /// Хранит данные в JSON-строке YG2.saves.am_data.
    /// В редакторе — fallback на PlayerPrefs.
    /// </summary>
    public static class SaveManager
    {
        private const string EditorKey = "am_save_data_v1";
        private const string YGField = "am_data";

        private static GameSaveData _data;

        /// <summary>Событие: данные перезагружены (например, после загрузки SDK).</summary>
        public static event Action OnDataReloaded;

        /// <summary>Актуальные данные. Загружаются лениво при первом обращении.</summary>
        public static GameSaveData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        /// <summary>Готовы ли данные (был ли Load).</summary>
        public static bool IsLoaded => _data != null;

        // ==================== ЗАГРУЗКА ====================

        /// <summary>Принудительная перезагрузка из хранилища.</summary>
        public static void Reload()
        {
            _data = null;
            Load();
            OnDataReloaded?.Invoke();
            Debug.Log("[SaveManager] Данные перезагружены");
        }

        private static void Load()
        {
            string json = ReadFromStorage();

            if (string.IsNullOrEmpty(json))
            {
                _data = new GameSaveData();
                Debug.Log("[SaveManager] Созданы дефолтные сохранения");
                return;
            }

            try
            {
                _data = JsonUtility.FromJson<GameSaveData>(json);
                if (_data == null) _data = new GameSaveData();
                Debug.Log($"[SaveManager] Загружено: money={_data.money}, inventory={_data.invIds.Count}, collection={_data.repairedIds.Count}, slots={_data.slotCount}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Ошибка парсинга JSON: {e.Message}");
                _data = new GameSaveData();
            }
        }

        // ==================== СОХРАНЕНИЕ ====================

        /// <summary>Сохранить всё в хранилище.</summary>
        public static void Save()
        {
            if (_data == null) return;

            string json;
            try
            {
                json = JsonUtility.ToJson(_data);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Ошибка сериализации: {e.Message}");
                return;
            }

            WriteToStorage(json);
        }

        // ==================== ХРАНИЛИЩЕ ====================

        private static string ReadFromStorage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                // YG2.saves заполняется плагином асинхронно.
                // Пока не готов — вернём то, что есть (пусто или дефолт).
                var saves = YG2.saves;
                if (saves != null)
                {
                    var json = saves.am_data;
                    if (!string.IsNullOrEmpty(json)) return json;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] YG2.saves недоступен: {e.Message}");
            }
            return null;
#else
            return PlayerPrefs.GetString(EditorKey, "");
#endif
        }

        private static void WriteToStorage(string json)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                var saves = YG2.saves;
                if (saves != null)
                {
                    saves.am_data = json;
                    YG2.SaveProgress();
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Не удалось записать в YG2: {e.Message}");
            }
            // Fallback на PlayerPrefs, если SDK недоступен
            PlayerPrefs.SetString(EditorKey, json);
            PlayerPrefs.Save();
#else
            PlayerPrefs.SetString(EditorKey, json);
            PlayerPrefs.Save();
#endif
        }

        // ==================== УТИЛИТЫ ====================

        /// <summary>Полный сброс (для отладки).</summary>
        public static void ResetAll()
        {
            _data = new GameSaveData();
            Save();
            Debug.Log("[SaveManager] Все сохранения сброшены");
        }
    }
}
