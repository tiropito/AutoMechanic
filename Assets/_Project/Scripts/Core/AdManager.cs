using System;
using UnityEngine;

#if UNITY_WEBGL && !UNITY_EDITOR
using BananaParty.YandexGames;
#endif

namespace AutoMechanic.Core
{
    /// <summary>
    /// Реклама через Yandex Games SDK.
    /// Rewarded Video — за просмотр даём $100.
    /// Останавливает звук игры на время показа.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [Header("Награды")]
        [SerializeField] private int rewardedMoney = 100;

        [Tooltip("Кулдаун между рекламами (сек)")]
        [SerializeField] private float cooldown = 30f;

        [Header("Отладка")]
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

        public void ShowRewardedForMoney()
        {
            if (!IsReady)
            {
                Debug.Log($"[AdManager] Кулдаун: {CooldownLeft:F0} сек");
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (YandexSDKInitializer.Instance == null || !YandexSDKInitializer.Instance.IsInitialized)
            {
                Debug.LogWarning("[AdManager] SDK не готов — выдаём награду как fallback");
                GiveReward();
                return;
            }
            ShowAdWebGL();
#else
            Debug.Log("[AdManager] Не WebGL — выдаём награду сразу");
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
                    Debug.Log("[AdManager] Реклама открыта — пауза звука");
                    PauseAllAudio();
                },
                onRewardedCallback: () =>
                {
                    Debug.Log("[AdManager] Досмотрено — награда");
                    GiveReward();
                },
                onCloseCallback: () =>
                {
                    Debug.Log("[AdManager] Реклама закрыта");
                    ResumeAllAudio();
                    if (Time.time - _lastShowTime > 1f)
                    {
                        _lastShowTime = Time.time;
                        OnRewardedFailed?.Invoke();
                    }
                },
                onErrorCallback: (error) =>
                {
                    Debug.LogError($"[AdManager] Ошибка: {error}");
                    ResumeAllAudio();
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

            Debug.Log($"[AdManager] +${rewardedMoney}");
            OnRewardedComplete?.Invoke();
        }

        // ==================== ЗВУК ====================

        private void PauseAllAudio()
        {
            AudioListener.pause = true;
            AudioListener.volume = 0f;
        }

        private void ResumeAllAudio()
        {
            AudioListener.pause = false;
            AudioListener.volume = 1f;
        }

        // ==================== ТЕСТЫ ====================

        [ContextMenu("ТЕСТ: показать рекламу")]
        private void TestShow() => ShowRewardedForMoney();

        [ContextMenu("ТЕСТ: сбросить кулдаун")]
        private void TestReset()
        {
            _lastShowTime = -999f;
            Debug.Log("[AdManager] Кулдаун сброшен");
        }
    }
}