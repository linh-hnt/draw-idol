using System.Collections.Generic;
using AFramework.UI;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ColorPickerPopup : BaseUIMenu
{
    public static string Identifier = "Popup/ColorPickerPopup";

    [Header("Color Picker")]
    [SerializeField] private ColorPickerController _colorPicker;
    [SerializeField] private Image _previewImage;

    [Header("Buttons")]
    [SerializeField] private Button _doneButton;
    [SerializeField] private Button _closeButton;

    [Header("Presets")]
    [SerializeField] private Transform _presetContainer;
    [SerializeField] private PresetSwatch _presetSwatchPrefab;

    [Header("Animation")]
    [SerializeField] private RectTransform _panelRoot;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Color _initialColor;
    private Color _currentColor;
    private readonly List<PresetSwatch> _presetSwatches = new List<PresetSwatch>();
    private Tween _showTween;
    private Tween _hideTween;
    private bool _isConfirming;

    private static readonly Color[] DefaultPresets =
    {
        Color.red, Color.green, Color.blue, Color.yellow,
        Color.cyan, Color.magenta, Color.white, Color.black,
        new Color(1f, 0.5f, 0f),
        new Color(0.5f, 0f, 0.5f),
        new Color(0.5f, 0.3f, 0.1f),
        new Color(0.7f, 0.7f, 0.7f),
    };

    private void Awake()
    {
        if (_colorPicker == null)
        {
            Debug.LogError($"[{Identifier}] ColorPickerController reference is null. Picker will not function.");
        }
        else
        {
            _colorPicker.onColorChanged.AddListener(OnColorPickerChanged);
        }

        if (_doneButton != null)
            _doneButton.onClick.AddListener(OnDoneClicked);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnDestroy()
    {
        if (_colorPicker != null)
            _colorPicker.onColorChanged.RemoveListener(OnColorPickerChanged);

        KillTweens();
    }

    public override void Init(object[] initParams)
    {
        base.Init(initParams);

        _initialColor = Color.red;
        if (initParams != null && initParams.Length > 0 && initParams[0] is Color initColor)
        {
            _initialColor = initColor;
        }

        _currentColor = _initialColor;
        _isConfirming = false;

        if (_colorPicker != null)
        {
            _colorPicker.SetColor(_initialColor);
        }

        if (_previewImage != null)
        {
            _previewImage.color = _initialColor;
        }

        BuildPresets(DefaultPresets);

        PlayShowAnimation();
    }

    private void OnColorPickerChanged(Color newColor)
    {
        _currentColor = newColor;
        if (_previewImage != null)
        {
            _previewImage.color = newColor;
        }
    }

    private void OnDoneClicked()
    {
        if (_isConfirming)
            return;

        _isConfirming = true;

        // EventManager.I.SetColorPickerAction?.Invoke(_currentColor);
        EventManager.I.SetPaintColorAction?.Invoke(_currentColor, -1);

        PlayHideAnimation(() => Pop());
    }

    private void OnCloseClicked()
    {
        PlayHideAnimation(() => Pop());
    }

    private void BuildPresets(Color[] presets)
    {
        if (presets == null || _presetSwatchPrefab == null || _presetContainer == null)
            return;

        for (int i = 0; i < presets.Length; i++)
        {
            if (i >= _presetSwatches.Count)
            {
                var swatch = Instantiate(_presetSwatchPrefab, _presetContainer);
                _presetSwatches.Add(swatch);
            }

            _presetSwatches[i].gameObject.SetActive(true);
            _presetSwatches[i].Init(presets[i], OnPresetSelected);
        }

        for (int i = presets.Length; i < _presetSwatches.Count; i++)
        {
            _presetSwatches[i].gameObject.SetActive(false);
        }
    }

    private void OnPresetSelected(Color presetColor)
    {
        _currentColor = presetColor;

        if (_colorPicker != null)
            _colorPicker.SetColor(presetColor);

        if (_previewImage != null)
            _previewImage.color = presetColor;
    }

    /// <summary>
    /// Animates the popup in: fades from transparent and scales up from 0.85 using OutBack easing.
    /// </summary>
    private void PlayShowAnimation()
    {
        KillTweens();

        if (_canvasGroup == null || _panelRoot == null)
            return;

        _canvasGroup.alpha = 0f;
        _panelRoot.localScale = Vector3.one * 0.85f;

        _showTween = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(1f, 0.25f).SetEase(Ease.OutCubic))
            .Join(_panelRoot.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
            .SetUpdate(true);
    }

    /// <summary>
    /// Animates the popup out: fades to transparent and scales down to 0.85 using InBack easing.
    /// When the animation completes, the provided callback is invoked (typically Pop()).
    /// </summary>
    private void PlayHideAnimation(System.Action onComplete)
    {
        KillTweens();

        if (_canvasGroup == null || _panelRoot == null)
        {
            onComplete?.Invoke();
            return;
        }

        _hideTween = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(0f, 0.2f).SetEase(Ease.InCubic))
            .Join(_panelRoot.DOScale(0.85f, 0.2f).SetEase(Ease.InBack))
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void KillTweens()
    {
        _showTween?.Kill();
        _hideTween?.Kill();
        _showTween = null;
        _hideTween = null;
    }
}
