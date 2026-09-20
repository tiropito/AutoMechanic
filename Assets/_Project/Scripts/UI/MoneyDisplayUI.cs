using TMPro;
using UnityEngine;
using AutoMechanic.Core;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Постоянный счётчик денег в углу экрана.
    /// Подписывается на EconomyManager.OnMoneyChanged.
    /// </summary>
    public class MoneyDisplayUI : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private TMP_Text moneyText;

        [Header("Форматирование")]
        [Tooltip("Префикс перед числом")]
        [SerializeField] private string prefix = "$";

        [Tooltip("Показывать короткий формат для тысяч: 1.2K, 1.5M")]
        [SerializeField] private bool useShortFormat = false;

        [Header("Анимация")]
        [Tooltip("Подсветить зелёным при пополнении")]
        [SerializeField] private Color gainColor = new Color(0.3f, 1f, 0.4f);

        [Tooltip("Подсветить красным при трате")]
        [SerializeField] private Color spendColor = new Color(1f, 0.4f, 0.4f);

        [SerializeField] private float flashDuration = 0.3f;

        private int _lastMoney = -1;
        private float _flashTimer;

        private void Start()
        {
            Subscribe();
            Refresh(instant: true);
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
        }

        private void Subscribe()
        {
            if (EconomyManager.Instance != null)
            {
                EconomyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
                EconomyManager.Instance.OnMoneyChanged += OnMoneyChanged;
            }
        }

        private void Update()
        {
            // Тик подсветки
            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && moneyText != null)
                    moneyText.color = Color.white;
            }
        }

        private void OnMoneyChanged(int newAmount)
        {
            if (_lastMoney >= 0)
            {
                if (newAmount > _lastMoney) Flash(gainColor);
                else if (newAmount < _lastMoney) Flash(spendColor);
            }

            _lastMoney = newAmount;
            Refresh(instant: false);
        }

        private void Refresh(bool instant)
        {
            if (moneyText == null) return;
            if (EconomyManager.Instance == null) return;

            int amount = EconomyManager.Instance.Money;
            moneyText.text = prefix + FormatNumber(amount);

            if (instant) _lastMoney = amount;
        }

        private string FormatNumber(int amount)
        {
            if (!useShortFormat) return amount.ToString();

            if (amount >= 1_000_000) return (amount / 1_000_000f).ToString("F1") + "M";
            if (amount >= 1_000) return (amount / 1_000f).ToString("F1") + "K";
            return amount.ToString();
        }

        private void Flash(Color color)
        {
            if (moneyText == null) return;
            moneyText.color = color;
            _flashTimer = flashDuration;
        }
    }
}