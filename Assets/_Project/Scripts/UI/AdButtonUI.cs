using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Обновляет кнопку рекламы: активна / на кулдауне.
    /// </summary>
    public class AdButtonUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private void Start()
        {
            if (button == null) button = GetComponent<Button>();
            if (label == null) label = GetComponentInChildren<TMP_Text>();
        }

        private void Update()
        {
            if (AdManager.Instance == null) return;

            bool ready = AdManager.Instance.IsReady;

            if (button != null) button.interactable = ready;

            if (label != null)
            {
                label.text = ready
                    ? $"+${AdManager.Instance.RewardedMoney} за рекламу"
                    : $"Ждите {Mathf.CeilToInt(AdManager.Instance.CooldownLeft)}с";
            }
        }
    }
}