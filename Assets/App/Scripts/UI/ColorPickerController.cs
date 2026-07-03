using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class ColorPickerEvent : UnityEvent<Color> { }

/// <summary>
/// Replacement for FlexibleColorPicker. Manages an SV (Saturation×Value) 2D picker square,
/// a Hue slider bar, and a preview image. Handles pointer down, drag, and up via
/// IPointerDownHandler/IDragHandler/IPointerUpHandler on the attached GameObject.
/// Fires onColorChanged whenever the color updates.
/// </summary>
public class ColorPickerController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Picker Images")]
    [SerializeField] private Image _svPickerImage;
    [SerializeField] private Image _huePickerImage;

    [Header("Markers")]
    [SerializeField] private RectTransform _svMarker;
    [SerializeField] private Image _previewSvImage;
    [SerializeField] private RectTransform _hueMarker;
    [SerializeField] private Image _previewHueImage;

    [Header("Events")]
    public ColorPickerEvent onColorChanged;

    private float _hue = 0f;        // 0..1
    private float _saturation = 1f; // 0..1
    private float _value = 1f;      // 0..1

    private Color _currentColor = Color.red;

    private bool _isDraggingSV;
    private bool _isDraggingHue;

    private Material _svMaterialInstance;

    private const string ShaderHueProperty = "_Hue";

    public Color Color
    {
        get => _currentColor;
        set => SetColor(value);
    }

    private void Awake()
    {
        if (_svPickerImage != null && _svPickerImage.material != null)
        {
            _svMaterialInstance = Instantiate(_svPickerImage.material);
            _svPickerImage.material = _svMaterialInstance;
        }

        SetColor(Color.red);
    }

    private void OnDestroy()
    {
        if (_svMaterialInstance != null)
        {
            Destroy(_svMaterialInstance);
            _svMaterialInstance = null;
        }
    }

    public void SetColor(Color color)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        _hue = h;
        _saturation = s;
        _value = v;
        _currentColor = color;
        ApplyToAll();
    }

    #region Pointer Handlers

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_svPickerImage != null)
        {
            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _svPickerImage.rectTransform, eventData.position, eventData.pressEventCamera, out localPos))
            {
                Vector2 normalized = RectPointToNormalized(localPos, _svPickerImage.rectTransform);
                if (normalized.x >= 0f && normalized.x <= 1f && normalized.y >= 0f && normalized.y <= 1f)
                {
                    _isDraggingSV = true;
                    _saturation = normalized.x;
                    _value = normalized.y;
                    ApplySV();
                    return;
                }
            }
        }

        if (_huePickerImage != null)
        {
            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _huePickerImage.rectTransform, eventData.position, eventData.pressEventCamera, out localPos))
            {
                Vector2 normalized = RectPointToNormalized(localPos, _huePickerImage.rectTransform);
                if (normalized.x >= 0f && normalized.x <= 1f)
                {
                    _isDraggingHue = true;
                    _hue = normalized.x;
                    ApplyHue();
                }
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_isDraggingSV && _svPickerImage != null)
        {
            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _svPickerImage.rectTransform, eventData.position, eventData.pressEventCamera, out localPos))
            {
                Vector2 normalized = RectPointToNormalized(localPos, _svPickerImage.rectTransform);
                _saturation = Mathf.Clamp01(normalized.x);
                _value = Mathf.Clamp01(normalized.y);
                ApplySV();
            }
        }
        else if (_isDraggingHue && _huePickerImage != null)
        {
            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _huePickerImage.rectTransform, eventData.position, eventData.pressEventCamera, out localPos))
            {
                Vector2 normalized = RectPointToNormalized(localPos, _huePickerImage.rectTransform);
                _hue = Mathf.Clamp01(normalized.x);
                ApplyHue();
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDraggingSV = false;
        _isDraggingHue = false;
    }

    #endregion

    #region Internal

    private void ApplySV()
    {
        UpdateCurrentColor();
        UpdateSVMarker();
        if (_previewSvImage != null)
            _previewSvImage.color = _currentColor;
        onColorChanged?.Invoke(_currentColor);
    }

    private void ApplyHue()
    {
        if (_svMaterialInstance != null)
            _svMaterialInstance.SetFloat(ShaderHueProperty, _hue);

        UpdateCurrentColor();
        UpdateHueMarker();
        if (_previewHueImage != null)
            _previewHueImage.color = _currentColor;
        onColorChanged?.Invoke(_currentColor);
    }

    private void ApplyToAll()
    {
        if (_svMaterialInstance != null)
            _svMaterialInstance.SetFloat(ShaderHueProperty, _hue);

        UpdateCurrentColor();
        UpdateSVMarker();
        UpdateHueMarker();
        if (_previewSvImage != null)
            _previewSvImage.color = _currentColor;

        if (_previewHueImage)
            _previewHueImage.color = _currentColor;

        onColorChanged?.Invoke(_currentColor);
    }

    private void UpdateCurrentColor()
    {
        _currentColor = Color.HSVToRGB(_hue, _saturation, _value);
    }

    private void UpdateSVMarker()
    {
        if (_svMarker == null || _svPickerImage == null)
            return;

        Rect rect = _svPickerImage.rectTransform.rect;

        Vector2 targetPosition = new Vector2(
                    rect.x + _saturation * rect.width,
                    rect.y + _value * rect.height
                );

        _svMarker.anchoredPosition = targetPosition;
        // Debug.LogError(new Vector2(_saturation, _value).ToString() + "\n" + new Vector2(rect.x, rect.y).ToString() + "\n" + targetPosition.ToString());
    }

    private void UpdateHueMarker()
    {
        if (_hueMarker == null || _huePickerImage == null)
            return;

        Rect rect = _huePickerImage.rectTransform.rect;
        float x = rect.x + _hue * rect.width;
        _hueMarker.anchoredPosition = new Vector2(x, _hueMarker.anchoredPosition.y);
    }

    /// <summary>
    /// Converts a local point within a RectTransform to 0..1 normalized coordinates.
    /// localPoint is relative to the pivot; rect.x/rect.y already encode the pivot offset
    /// (e.g., pivot 0.5 on a 100-wide rect gives rect.x = -50).
    /// So (localPoint.x - rect.x) / rect.width gives 0 at left edge and 1 at right edge.
    /// </summary>
    private static Vector2 RectPointToNormalized(Vector2 localPoint, RectTransform rect)
    {
        Rect r = rect.rect;
        return new Vector2(
            (localPoint.x - r.x) / r.width,
            (localPoint.y - r.y) / r.height
        );
    }

    #endregion
}
