using UnityEngine;
using UnityEngine.UI;
using AutoMechanic.Core;

namespace AutoMechanic.UI
{
    /// <summary>Автоматически добавляет звук клика на кнопку.</summary>
    [RequireComponent(typeof(Button))]
    public class ButtonSound : MonoBehaviour
    {
        private void Start()
        {
            var btn = GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlayClick();
            });
        }
    }
}