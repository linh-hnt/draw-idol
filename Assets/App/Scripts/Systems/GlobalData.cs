using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AFramework;

public class GlobalData : ManualSingletonMono<GlobalData>
{
    [Header("UI_Material")]
    [SerializeField] private Material _uiGrayMaterial;

    [Header("Text Mesh Pro")]
    [SerializeField] private List<TextMaterialData> _uiTextMaterialData = new List<TextMaterialData>();

    public Material UIGrayMaterial => _uiGrayMaterial;

    public Material GetUITextMterial(TextColorStatus textColorStatus)
    {
        TextMaterialData currentTextData = _uiTextMaterialData.Find(data => data.textColorStatus == textColorStatus);

        if (currentTextData != null)
        {
            return currentTextData.textColorMaterial;
        }
        else
        {
            return null;
        }
    }
}

[System.Serializable]
public class TextMaterialData
{
    public TextColorStatus textColorStatus;
    public Material textColorMaterial;
}

public enum TextColorStatus
{
    Blue,
    Pink,
    Purple,
    Yellow,
}
