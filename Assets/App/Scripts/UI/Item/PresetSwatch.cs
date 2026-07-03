using UnityEngine;
using UnityEngine.UI;
using System;

public class PresetSwatch : MonoBehaviour
{
    [SerializeField] private Image _swatchImage;
    [SerializeField] private Button _button;

    private Color _swatchColor;
    private Action<Color> _onSelected;

    public void Init(Color color, Action<Color> onSelected)
    {
        _swatchColor = color;
        _onSelected = onSelected;
        _swatchImage.color = color;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnSwatchClicked);
    }

    private void OnSwatchClicked()
    {
        _onSelected?.Invoke(_swatchColor);
    }
}
