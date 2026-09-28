using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using BananaParty.YandexGames;
#endif

namespace AutoMechanic.Core
{
    /// <summary>
    /// Инициализация Yandex Games SDK. Улучшенная версия:
    ///  - таймаут на Initialize (10 сек), чтобы не висеть, если SDK не ответил;
    ///  - подробное логирование состояния;
    ///  - корректный fallback: если SDK не загрузился — игра всё равно запустится;
    ///  - GameReady() вызывается только после успешной инициализации.
    /// </summary>
    public class YandexSDKInitializer : MonoBehaviour
    {
        public static YandexSDKInitializer Instance { get; private set; }

        /// <summary>Готов ли SDK к работе.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Инициализация упала (SDK не найден / таймаут).</summary>
        public bool InitializationFailed { get; private set; }

        /// <summary>Код языка пользователя (ru, en, tr, ...).</summary>
        public string UserLanguage { get; private set; } = "ru";

        private const string LangSaveKey = "am_user_lang";
        private const float InitTimeoutSeconds = 10f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            UserLanguage = PlayerPrefs.GetString(LangSaveKey, "ru");
        }

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(InitRoutine());
#else
            Debug.Log("[YandexSDK] Не WebGL — SDK не инициализируем");
            IsInitialized = false;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator InitRoutine()
        {
            Debug.Log("[YandexSDK] ▶ Инициализация SDK...");

            bool callbackFired = false;

            try
            {
                YandexGamesSdk.Initialize(() => { callbackFired = true; });
            }
            catch (Exception e)
            {
                Debug.LogError($"[YandexSDK] ✗ Ошибка вызова Initialize: {e.Message}\n{e.StackTrace}");
                InitializationFailed = true;
                yield break;
            }

            // Ждём callback с таймаутом
            float elapsed = 0f;
            while (!callbackFired && elapsed < InitTimeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!callbackFired)
            {
                Debug.LogWarning($"[YandexSDK] ✗ Callback не сработал за {InitTimeoutSeconds}с. " +
                                 "Проверь, что SDK-скрипт подключён в HTML и нет ошибок в консоли браузера.");
                InitializationFailed = true;
                yield break;
            }

            Debug.Log("[YandexSDK] ✓ SDK инициализирован");
            IsInitialized = true;

            // 1. Определяем язык
            DetectLanguage();

            // 2. Сообщаем платформе, что игра готова
            SendGameReady();
        }

        private void SendGameReady()
        {
            try
            {
                YandexGamesSdk.GameReady();
                Debug.Log("[YandexSDK] ✓ GameReady отправлен");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[YandexSDK] GameReady ошибка: {e.Message}");
            }
        }

        /// <summary>
        /// Определяет язык через SDK.
        /// Сначала — прямой API, потом — рефлексия на случай другой версии.
        /// </summary>
        private void DetectLanguage()
        {
            string lang = null;

            // ===== Вариант 1: прямой доступ =====
            try
            {
                lang = YandexGamesSdk.Environment.i18n.lang;
                if (!string.IsNullOrEmpty(lang))
                    Debug.Log($"[YandexSDK] Язык через прямой API: {lang}");
            }
            catch (Exception e)
            {
                Debug.Log($"[YandexSDK] Прямой API недоступен: {e.Message}. Пробуем рефлексию.");
            }

            // ===== Вариант 2: рефлексия =====
            if (string.IsNullOrEmpty(lang))
            {
                try
                {
                    var sdkType = typeof(YandexGamesSdk);
                    var envProp = sdkType.GetProperty("Environment", BindingFlags.Public | BindingFlags.Static);
                    if (envProp != null)
                    {
                        var env = envProp.GetValue(null);
                        if (env != null)
                        {
                            var i18nProp = env.GetType().GetProperty("I18n") ?? env.GetType().GetProperty("i18n");
                            if (i18nProp != null)
                            {
                                var i18n = i18nProp.GetValue(env);
                                if (i18n != null)
                                {
                                    var langProp = i18n.GetType().GetProperty("Lang") ?? i18n.GetType().GetProperty("lang");
                                    if (langProp != null) lang = langProp.GetValue(i18n) as string;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[YandexSDK] Рефлексия не сработала: {e.Message}");
                }
            }

            if (!string.IsNullOrEmpty(lang))
            {
                UserLanguage = lang;
                PlayerPrefs.SetString(LangSaveKey, lang);
                PlayerPrefs.Save();
                Debug.Log($"[YandexSDK] Язык пользователя: {lang}");
            }
            else
            {
                UserLanguage = "ru";
                Debug.LogWarning("[YandexSDK] Язык не определён — fallback на 'ru'");
            }
        }
#endif

        /// <summary>Публичный метод: текущий язык для UI.</summary>
        public string GetLanguage()
        {
            return string.IsNullOrEmpty(UserLanguage) ? "ru" : UserLanguage;
        }

        /// <summary>Удобный флаг: SDK точно готов и можно звать VideoAd / Leaderboard.</summary>
        public bool CanUseSdk => IsInitialized && !InitializationFailed;
    }
}
