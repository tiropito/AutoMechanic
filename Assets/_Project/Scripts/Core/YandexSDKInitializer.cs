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

            // Сразу подгружаем последний известный язык (на случай если SDK не отработает)
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
            YandexGamesSdk.Initialize(OnSdkInitialized);
        }

        private void OnSdkInitialized()
        {
            Debug.Log("[YandexSDK] Инициализирован успешно");
            IsInitialized = true;

            // Определяем язык пользователя (требование модерации Яндекса)
            DetectLanguage();

            // Сообщаем платформе, что игра готова
            YandexGamesSdk.GameReady();
        }

        /// <summary>
        /// Пытается достать язык через SDK.
        /// Работает через рефлексию — не падает, если API в этой версии другое.
        /// </summary>
        private void DetectLanguage()
        {
            string lang = null;

            try
            {
                // Вариант 1: YandexGamesSdk.Environment.I18n.Lang
                var sdkType = typeof(YandexGamesSdk);
                var envProp = sdkType.GetProperty("Environment", BindingFlags.Public | BindingFlags.Static);
                if (envProp != null)
                {
                    var env = envProp.GetValue(null);
                    if (env != null)
                    {
                        var i18nProp = env.GetType().GetProperty("I18n");
                        if (i18nProp != null)
                        {
                            var i18n = i18nProp.GetValue(env);
                            if (i18n != null)
                            {
                                var langProp = i18n.GetType().GetProperty("Lang");
                                if (langProp != null) lang = langProp.GetValue(i18n) as string;
                            }
                        }
                    }
                }

                // Вариант 2: YandexGamesSdk.GetLanguage() — если есть метод
                if (string.IsNullOrEmpty(lang))
                {
                    var getLangMethod = sdkType.GetMethod("GetLanguage",
                        BindingFlags.Public | BindingFlags.Static);
                    if (getLangMethod != null)
                        lang = getLangMethod.Invoke(null, null) as string;
                }

                // Вариант 3: YandexGamesSdk.Language — если есть свойство
                if (string.IsNullOrEmpty(lang))
                {
                    var langProp2 = sdkType.GetProperty("Language",
                        BindingFlags.Public | BindingFlags.Static);
                    if (langProp2 != null)
                        lang = langProp2.GetValue(null) as string;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[YandexSDK] Не удалось получить язык через SDK: {e.Message}");
            }

            if (!string.IsNullOrEmpty(lang))
            {
                UserLanguage = lang;
                PlayerPrefs.SetString(LangSaveKey, lang);
                PlayerPrefs.Save();
                Debug.Log($"[YandexSDK] Язык пользователя определён: {lang}");
            }
            else
            {
                Debug.LogWarning("[YandexSDK] Язык не определён — fallback на 'ru'");
                UserLanguage = "ru";
            }
        }
#endif

        /// <summary>
        /// Публичный метод: получить текущий язык. Использовать в UI.
        /// </summary>
        public string GetLanguage()
        {
            return string.IsNullOrEmpty(UserLanguage) ? "ru" : UserLanguage;
        }
    }
}