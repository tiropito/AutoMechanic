using System;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Летящий текст +$X или -$X от машины к счётчику денег.
    /// </summary>
    public class MoneyFlyText : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup canvasGroup;

        private Action _onComplete;

        public void Fly(int amount, Vector2 startPos, Vector2 endPos, Action onComplete)
        {
            _onComplete = onComplete;

            bool isGain = amount > 0;
            int absAmount = Mathf.Abs(amount);

            if (label != null)
            {
                label.text = isGain ? $"+${absAmount}" : $"-${absAmount}";
                label.color = isGain
                    ? new Color(0.3f, 1f, 0.4f)   // зелёный при доходе
                    : new Color(1f, 0.4f, 0.4f);  // красный при трате
            }

            var rt = GetComponent<RectTransform>();
            if (rt == null) { Destroy(gameObject); return; }

            rt.anchoredPosition = startPos;

            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            float duration = 0.85f;

            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOScale(1.25f, 0.15f).SetEase(Ease.OutBack));
            seq.Join(canvasGroup != null ? canvasGroup.DOFade(1f, 0.15f) : DOTween.Sequence());
            seq.Append(rt.DOAnchorPos(endPos, duration).SetEase(Ease.InBack));
            seq.Join(rt.DOScale(0.5f, duration * 0.6f).SetEase(Ease.InQuad).SetDelay(duration * 0.4f));
            seq.Join(canvasGroup != null ? canvasGroup.DOFade(0f, duration * 0.5f).SetEase(Ease.InQuad).SetDelay(duration * 0.5f) : DOTween.Sequence());

            seq.OnComplete(() =>
            {
                _onComplete?.Invoke();
                Destroy(gameObject);
            });
        }
    }
}