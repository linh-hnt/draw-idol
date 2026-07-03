using System.Collections;
using System.Collections.Generic;
using AFramework.UI;
using UnityEngine;
using UnityEngine.UI;

public class SelectColorPopup : BaseUIMenu
{
    public static string Identifier = "Popup/SelectColorPopup";

    [Header("Finish Draw")]
    [SerializeField] private GameObject _finishDrawObject;
    [SerializeField] private Button _finishDrawButton;

    [Header("Select Color")]
    [SerializeField] private GameObject _selectColorObject;
    [SerializeField] private Transform _selectColorItemContain;
    [SerializeField] private SelectColorItem _selectColorItemPrefab;
    [SerializeField] private Button _pickColorButton;

    private List<SelectColorItem> _selectColorItems = new List<SelectColorItem>();

    private int _correctItemIndex = -1;

    private void Awake()
    {
        _finishDrawButton.onClick.AddListener(OnClickFinishDrawButton);
        _pickColorButton.onClick.AddListener(OnClickPickColorButton);
    }

    public override void Init(object[] initParams)
    {
        base.Init(initParams);

        ColorContent colorContent = null;
        bool hasShowFinishPaint = true;
        if (initParams != null)
        {
            colorContent = (ColorContent)initParams[0];
            hasShowFinishPaint = (bool)initParams[1];
        }

        if (colorContent != null)
        {
            _correctItemIndex = colorContent.indexCorrect;
            UpdateSelectColor(colorContent.colors);
        }

        if (hasShowFinishPaint)
        {
            _finishDrawObject.SetActive(true);
            _selectColorObject.SetActive(false);
        }
        else
        {
            _finishDrawObject.SetActive(false);
            _selectColorObject.SetActive(true);
        }
    }

    private void UpdateSelectColor(List<Color> selectColors)
    {
        if (selectColors == null || selectColors.Count <= 0)
            return;

        int itemsCount = _selectColorItems.Count;
        SelectColorItem currentSelectItem = null;
        for (int i = itemsCount, length = selectColors.Count; i < length; i++)
        {
            currentSelectItem = Instantiate(_selectColorItemPrefab, _selectColorItemContain);
            _selectColorItems.Add(currentSelectItem);
        }

        for (int i = 0, length = _selectColorItems.Count; i < length; i++)
        {
            currentSelectItem = _selectColorItems[i];

            currentSelectItem.gameObject.SetActive(i < selectColors.Count);
            if (i < selectColors.Count)
            {
                currentSelectItem.InitItem(selectColors[i], i);
            }
        }
    }

    private void OnClickFinishDrawButton()
    {
        ShowSelectColor();

        GameManager.I.DoVibration();

        EventManager.I.FinishPaintAction?.Invoke();
    }

    private void ShowSelectColor()
    {
        _finishDrawObject.SetActive(false);

        _selectColorObject.SetActive(true);
    }

    private void OnClickPickColorButton()
    {
        CanvasManager.Push(ColorPickerPopup.Identifier, null);
    }
}
