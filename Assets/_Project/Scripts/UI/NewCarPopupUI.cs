using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Data;
using AutoMechanic.Gameplay;
using DG.Tweening;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Всплывающее окно "Новая машина!" при первом ремонте машины.
    /// Показывается из CollectionManager.OnCollectionChanged.
    /// </summary>
    public class NewCarPopupUI : MonoBehaviour
    {
        public static NewCarPopupUI Instance { get; private set; }

        [Header("Ссылки")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Image carImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text carNameText;
        [SerializeField] private Button closeButton;

        [Header("Анимация")]
        [SerializeField] private float popDuration = 0.5f;
        [SerializeField] private float stayDuration = 2.2f;

        [Header("Звёзды")]
            [SerializeField] private TMPro.TMP_Text[] stars;

        private string _lastShownCarId;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (rootPanel != null) rootPanel.SetActive(false);

            if (CollectionManager.Instance != null)
                CollectionManager.Instance.OnCollectionChanged += OnCollectionChanged;
        }

        private void OnDestroy()
        {
            if (CollectionManager.Instance != null)
                CollectionManager.Instance.OnCollectionChanged -= OnCollectionChanged;
        }

        private void OnCollectionChanged()
        {
            // Открываем попап только для новой машины
            // Т.к. событие не передаёт car, надо узнать через getrepaired count
            // Проще: RepairManager вызывает Show(car) напрямую
        }

        /// <summary>Показать попап для конкретной машины.</summary>
        public void Show(CarData car)
        {
            if (car == null) return;
            if (_lastShownCarId == car.id) return; // не показывать дважды
            _lastShownCarId = car.id;

            if (rootPanel != null) rootPanel.SetActive(true);

            if (carImage != null)
            {
                carImage.sprite = car.sprite;
                carImage.color = car.sprite != null ? Color.white : new Color(1, 1, 1, 0);
            }
            if (titleText != null) titleText.text = "НОВАЯ МАШИНА!";
            if (carNameText != null)
            {
                carNameText.text = car.displayName;
                carNameText.color = car.RarityColor;
            }

            PlayAppearAnimation();
        }

        private void PlayAppearAnimation()
        {
            var rt = rootPanel != null ? rootPanel.GetComponent<RectTransform>() : null;
            if (rt != null)
            {
                rt.localScale = Vector3.one * 0.5f;
                rt.DOScale(1f, popDuration).SetEase(Ease.OutBack);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, popDuration * 0.6f).SetEase(Ease.OutQuad);
            }

            // Звёзды разлетаются
            if (stars != null)
            {
                foreach (var star in stars)
                {
                    if (star == null) continue;

                    var starRt = star.rectTransform;
                    star.gameObject.SetActive(true);
                    starRt.localScale = Vector3.zero;
                    starRt.anchoredPosition = Vector2.zero;

                    var c = star.color;
                    c.a = 1f;
                    star.color = c;

                    Vector2 target = new Vector2(
                        UnityEngine.Random.Range(-250f, 250f),
                        UnityEngine.Random.Range(-180f, 180f));

                    float delay = UnityEngine.Random.Range(0f, 0.2f);

                    Sequence s = DOTween.Sequence();
                    s.Append(starRt.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(delay));
                    s.Join(starRt.DOAnchorPos(target, 0.6f).SetEase(Ease.OutCubic).SetDelay(delay));
                    s.Append(star.DOFade(0f, 0.4f));
                }
            }

            // Автозакрытие
            CancelInvoke(nameof(Hide));
            Invoke(nameof(Hide), stayDuration);
        }

        public void Hide()
        {
            CancelInvoke(nameof(Hide));

            if (rootPanel == null) return;

            var rt = rootPanel.GetComponent<RectTransform>();
            if (rt != null)
                rt.DOScale(0.5f, 0.25f).SetEase(Ease.InBack);

            if (canvasGroup != null)
                canvasGroup.DOFade(0f, 0.25f).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    if (rootPanel != null) rootPanel.SetActive(false);
                });
        }
    }
}