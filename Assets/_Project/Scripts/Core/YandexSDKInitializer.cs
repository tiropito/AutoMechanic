using System.Reflection;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using BananaParty.YandexGames;
#endif

namespace AutoMechanic.Core
{
    /// <summary>
    /// Инициализация Yandex Games SDK при старте игры.
    /// Определяет язык пользователя (обязательное требование модерации).
    /// Сообщает платформе GameReady.
    /// Вне WebGL — ничего не делает, чтобы не ломать редактор.
    /// </summary>
    public class YandexSDKInitializer : MonoBehaviour
    {
        public static YandexSDKInitializer Instance { get; private set; }

        /// <summary>Готов ли SDK к работе.</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>Код языка пользователя (ru, en, tr, ...).</summary>
        public string UserLanguage { get; private set; } = "ru";

        private const string LangSaveKey = "am_user_lang";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Подгружаем последний известный язык (на случай, если SDK не отработает)
            UserLanguage = PlayerPrefs.GetString(LangSaveKey, "ru");
        }

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            InitSDK();
#else
            Debug.Log("[YandexSDK] Не WebGL — SDK не инициализируем");
            IsInitialized = false;
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void InitSDK()
        {
            Debug.Log("[YandexSDK] Инициализация...");

            try
            {
                YandexGamesSdk.Initialize(OnSdkInitialized);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[YandexSDK] Ошибка инициализации: {e.Message}");
                IsInitialized = false;
            }
        }

        private void OnSdkInitialized()
        {
            Debug.Log("[YandexSDK] Инициализирован успешно");
            IsInitialized = true;

            // 1. Определяем язык пользователя (требование модерации)
            DetectLanguage();

            // 2. Сообщаем платформе, что игра готова
            try
            {
                YandexGamesSdk.GameReady();
                Debug.Log("[YandexSDK] GameReady отправлен");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[YandexSDK] GameReady ошибка: {e.Message}");
            }
        }

        /// <summary>
        /// Определяет язык через SDK.
        /// Сначала пробует прямой API, потом — рефлексию (для совместимости).
        /// </summary>
        private void DetectLanguage()
        {
            string lang = null;

            // ===== Вариант 1: прямой доступ к BananaParty API =====
            try
            {
                lang = YandexGamesSdk.Environment.i18n.lang;
                if (!string.IsNullOrEmpty(lang))
                    Debug.Log($"[YandexSDK] Язык через прямой API: {lang}");
            }
            catch (System.Exception e)
            {
                Debug.Log($"[YandexSDK] Прямой API недоступен: {e.Message}. Пробуем рефлексию.");
            }

            // ===== Вариант 2: рефлексия (на случай другой версии SDK) =====
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
                            var i18nProp = env.GetType().GetProperty("I18n");
                            if (i18nProp == null) i18nProp = env.GetType().GetProperty("i18n");
                            if (i18nProp != null)
                            {
                                var i18n = i18nProp.GetValue(env);
                                if (i18n != null)
                                {
                                    var langProp = i18n.GetType().GetProperty("Lang");
                                    if (langProp == null) langProp = i18n.GetType().GetProperty("lang");
                                    if (langProp != null) lang = langProp.GetValue(i18n) as string;
                                }
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[YandexSDK] Рефлексия не сработала: {e.Message}");
                }
            }

            // ===== Применяем результат =====
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
    }
}