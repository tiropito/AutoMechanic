using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Конфетти — разлетающиеся цветные прямоугольники при завершении заказа.
    /// </summary>
    public class ConfettiUI : MonoBehaviour
    {
        public static ConfettiUI Instance { get; private set; }

        [Header("Настройки")]
        [SerializeField] private RectTransform spawnArea;
        [SerializeField] private int count = 25;
        [SerializeField] private float lifetime = 1.8f;
        [SerializeField] private float minSize = 10f;
        [SerializeField] private float maxSize = 22f;

        private static readonly Color[] Colors = new Color[]
        {
            new Color(1f, 0.3f, 0.3f),   // красный
            new Color(1f, 0.85f, 0.3f),  // жёлтый
            new Color(0.4f, 1f, 0.4f),   // зелёный
            new Color(0.4f, 0.7f, 1f),   // синий
            new Color(0.85f, 0.5f, 1f),  // фиолетовый
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>Запустить конфетти из указанной позиции.</summary>
        public void Play(Vector2 startPos)
        {
            if (spawnArea == null) return;

            for (int i = 0; i < count; i++)
                SpawnPiece(startPos);
        }

        private void SpawnPiece(Vector2 startPos)
        {
            var go = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(spawnArea, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(
                Random.Range(minSize, maxSize),
                Random.Range(minSize, maxSize) * 0.5f);
            rt.anchoredPosition = startPos;
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            var img = go.GetComponent<Image>();
            img.color = Colors[Random.Range(0, Colors.Length)];
            img.raycastTarget = false;

            // Случайное направление
            Vector2 targetPos = startPos + new Vector2(
                Random.Range(-350f, 350f),
                Random.Range(150f, 500f));

            float duration = Random.Range(1.2f, lifetime);

            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOAnchorPos(targetPos, duration).SetEase(Ease.OutCubic));
            seq.Join(rt.DORotate(new Vector3(0, 0, Random.Range(-720f, 720f)), duration, RotateMode.FastBeyond360));
            seq.Join(rt.DOScale(0.3f, duration * 0.6f).SetDelay(duration * 0.4f));

            if (img != null)
                seq.Join(img.DOFade(0f, duration * 0.5f).SetDelay(duration * 0.5f));

            seq.OnComplete(() => Destroy(go));
        }
    }
}