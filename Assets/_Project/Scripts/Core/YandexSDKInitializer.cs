using System;
using UnityEngine;
using YG;

namespace AutoMechanic.Core
{
    /// <summary>
    /// Инициализация SDK Яндекс Игр через Plugin Your Games 2.0.
    /// Плагин делает всё сам — этот скрипт только ждёт готовности и логирует.
    /// </summary>
    public class YandexSDKInitializer : MonoBehaviour
    {
        public static YandexSDKInitializer Instance { get; private set; }

        public bool IsInitialized { get; private set; }
        public string UserLanguage { get; private set; } = "ru";

        private const string LangSaveKey = "am_user_lang";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            UserLanguage = PlayerPrefs.GetString(LangSaveKey, "ru");
        }

        private void Start()
        {
            Invoke(nameof(CheckLanguage), 1f);
            IsInitialized = true;
            Debug.Log("[YandexSDK] ✓ Готов (инициализацией занимается PluginYG2)");
        }

        private void CheckLanguage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string lang = YG2.envir.language;
                if (!string.IsNullOrEmpty(lang))
                {
                    UserLanguage = lang;
                    PlayerPrefs.SetString(LangSaveKey, lang);
                    PlayerPrefs.Save();
                    Debug.Log($"[YandexSDK] Язык пользователя: {lang}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[YandexSDK] Не удалось получить язык: {e.Message}");
            }
#else
            Debug.Log("[YandexSDK] Не WebGL — язык не запрашиваем");
#endif
        }

        public string GetLanguage()
        {
            return string.IsNullOrEmpty(UserLanguage) ? "ru" : UserLanguage;
        }

        public bool CanUseSdk => IsInitialized;
    }
}