using System;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using BananaParty.YandexGames;
#endif

namespace AutoMechanic.Core
{
    /// <summary>
    /// Реклама через Yandex Games SDK (BananaParty).
    /// Rewarded Video — за просмотр даём деньги.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("Награды")]
        [Tooltip("Сколько денег за просмотр рекламы")]
        [SerializeField] private int rewardedMoney = 100;

        [Tooltip("Кулдаун между рекламами (сек)")]
        [SerializeField] private float cooldown = 60f;

        [Header("Отладка")]
        [Tooltip("Логировать колбэки SDK (только WebGL)")]
        [SerializeField] private bool logCallbacks = true;

        private float _lastShowTime = -999f;

        public event Action OnRewardedComplete;
        public event Action OnRewardedFailed;

        public int RewardedMoney => rewardedMoney;
        public bool IsReady => Time.time - _lastShowTime >= cooldown;
        public float CooldownLeft => Mathf.Max(0f, cooldown - (Time.time - _lastShowTime));

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>Показать рекламу за деньги.</summary>
        public void ShowRewardedForMoney()
        {
            if (!IsReady)
            {
                Debug.Log($"[AdManager] Кулдаун: осталось {CooldownLeft:F0} сек");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // На localhost SDK не инициализирован — не дёргаем API, чтобы не было рекурсии
            if (YandexSDKInitializer.Instance == null || !YandexSDKInitializer.Instance.IsInitialized)
            {
                Debug.LogWarning("[AdManager] SDK не готов (localhost?) — выдаём награду как fallback");
                GiveReward();
                return;
            }

            ShowAdWebGL();
#else
            Debug.Log("[AdManager] Не WebGL — выдаём награду сразу (тестовый режим)");
            GiveReward();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void ShowAdWebGL()
        {
            Debug.Log("[AdManager] Запрос Rewarded Video...");

            if (logCallbacks)
                YandexGamesSdk.CallbackLogging = true;

            VideoAd.Show(
                onOpenCallback: () =>
                {
                    Debug.Log("[AdManager] Реклама открыта");
                },
                onRewardedCallback: () =>
                {
                    Debug.Log("[AdManager] Игрок досмотрел — выдаём награду");
                    GiveReward();
                },
                onCloseCallback: () =>
                {
                    Debug.Log("[AdManager] Реклама закрыта");
                    // Если награду не дали (не досмотрел) — обновляем кулдаун без награды
                    if (Time.time - _lastShowTime > 1f)
                    {
                        _lastShowTime = Time.time;
                        OnRewardedFailed?.Invoke();
                    }
                },
                onErrorCallback: (error) =>
                {
                    Debug.LogError($"[AdManager] Ошибка рекламы: {error}");
                    _lastShowTime = Time.time;
                    OnRewardedFailed?.Invoke();
                }
            );
        }
#endif

        private void GiveReward()
        {
            _lastShowTime = Time.time;

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Add(rewardedMoney);

            Debug.Log($"[AdManager] +${rewardedMoney} за рекламу");
            OnRewardedComplete?.Invoke();
        }

        // ===== ТЕСТЫ (ПКМ по компоненту) =====
        [ContextMenu("ТЕСТ: показать рекламу")]
        private void TestShow() => ShowRewardedForMoney();

        [ContextMenu("ТЕСТ: сбросить кулдаун")]
        private void TestResetCooldown()
        {
            _lastShowTime = -999f;
            Debug.Log("[AdManager] Кулдаун сброшен");
        }
    }
}