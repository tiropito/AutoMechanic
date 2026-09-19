using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoMechanic.UI
{
    /// <summary>
    /// Один таб переключения между машинами в гараже.
    /// </summary>
    public class SessionTabUI : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;

        [Header("Цвета")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.6f, 1f, 0.5f);

        private int _index;
        private Action<int> _onClick;

        public void Bind(int index, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClick?.Invoke(_index));
            }
        }

        public void Refresh(string carName, bool isSelected)
        {
            if (label != null) label.text = carName;
            if (background != null) background.color = isSelected ? selectedColor : normalColor;
        }
    }
}