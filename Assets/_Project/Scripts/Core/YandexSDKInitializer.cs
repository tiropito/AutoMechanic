using System;
using System.Collections;
using UnityEngine;
using YG;

namespace AutoMechanic.Core
{
    /// <summary>
    /// Инициализация SDK Яндекс Игр через Plugin Your Games 2.0.
    /// Дожидается готовности YG2, потом перезагружает сохранения.
    /// </summary>
    public class YandexSDKInitializer : MonoBehaviour
    {
        public static YandexSDKInitializer Instance { get; private set; }

        public bool IsInitialized { get; private set; }
        public string UserLanguage { get; private set; } = "ru";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Первичная загрузка (может быть дефолт, если SDK не готов)
            _ = SaveManager.Data;
            UserLanguage = SaveManager.Data.lang;
        }

        private void Start()
        {
            StartCoroutine(InitRoutine());
        }

        private IEnumerator InitRoutine()
        {
            Debug.Log("[YandexSDK] ▶ Ожидание готовности SDK...");

            float elapsed = 0f;
            const float timeout = 15f;

            // Ждём, пока плагин сообщит о готовности
            while (!YG2.isSDKEnabled && elapsed < timeout)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (YG2.isSDKEnabled)
            {
                Debug.Log("[YandexSDK] ✓ SDK готов. Перезагружаем сохранения.");
                SaveManager.Reload();
                TryDetectLanguage();
            }
            else
            {
                Debug.LogWarning($"[YandexSDK] SDK не готов за {timeout}с — работаем на дефолтах");
            }
#else
            Debug.Log("[YandexSDK] Не WebGL — SDK не инициализируем");
#endif

            IsInitialized = true;
        }

        private void TryDetectLanguage()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                string lang = YG2.envir.language;
                if (!string.IsNullOrEmpty(lang))
                {
                    UserLanguage = lang;
                    SaveManager.Data.lang = lang;
                    SaveManager.Save();
                    Debug.Log($"[YandexSDK] Язык: {lang}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[YandexSDK] Язык не определён: {e.Message}");
            }
#endif
        }

        public string GetLanguage() => string.IsNullOrEmpty(UserLanguage) ? "ru" : UserLanguage;
        public bool CanUseSdk => IsInitialized;
    }
}
