using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using AFramework.UI;

public class SelectColorItem : MonoBehaviour
{
    [SerializeField] private Image imgColor;

    [SerializeField] private Button btnColor;

    private int index;
    private Color _itemColor;

    public void InitItem(Color color, int index)
    {
        imgColor.color = color;
        this.index = index;
        _itemColor = color;
    }

    private void Start()
    {
        btnColor.onClick.AddListener(OnBtnColorHandle);
    }

    private void OnBtnColorHandle()
    {
        EventManager.I.SetPaintColorAction?.Invoke(_itemColor, index);
    }
}
