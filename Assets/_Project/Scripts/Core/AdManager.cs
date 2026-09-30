using System;
using UnityEngine;
using YG;

namespace AutoMechanic.Core
{
    /// <summary>
    /// Реклама через Plugin Your Games 2.0.
    /// Rewarded — за просмотр даём $100.
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

            Debug.Log("[AdManager] ▶ Запрос Rewarded Video...");

#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                YG2.RewardedAdvShow("reward_money", OnRewardedSuccess);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AdManager] Ошибка вызова рекламы: {e.Message}");
                GiveReward();
            }
#else
            Debug.Log("[AdManager] Не WebGL — выдаём награду сразу");
            GiveReward();
#endif
        }

        private void OnRewardedSuccess()
        {
            Debug.Log("[AdManager] ✓ Rewarded просмотрен — выдаём награду");
            GiveReward();
        }

        private void GiveReward()
        {
            _lastShowTime = Time.time;

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.Add(rewardedMoney);

            Debug.Log($"[AdManager] +${rewardedMoney}");
            OnRewardedComplete?.Invoke();
        }

        // ==================== ЗВУК ====================

        public void PauseAllAudio()
        {
            AudioListener.pause = true;
            AudioListener.volume = 0f;
        }

        public void ResumeAllAudio()
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