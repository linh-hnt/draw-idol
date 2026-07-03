using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AFramework.UI;
using UnityEngine.UI;

public class FinishLevelPopup : BaseUIMenu
{
    public static string Identifier = "Popup/FinishLevelPopup";

    [SerializeField] private Button _closeButton;

    private void Awake()
    {
        _closeButton.onClick.AddListener(OnClickCloseButton);
    }

    private void OnClickCloseButton()
    {
        GameManager.I.DoVibration();

        CanvasManager.PopAllLayer(eUILayer.Popup);

        CanvasManager.Push(WinPopup.Identifier, null);
    }
}
