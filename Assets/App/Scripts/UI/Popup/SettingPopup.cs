using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AFramework.UI;
using UnityEngine.UI;

public class SettingPopup : BaseUIMenu
{
    public static string Identifier = "Popup/SettingPopup";

    [SerializeField] private Button _policyButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _popupButton;

    private void Start()
    {
        _closeButton.onClick.AddListener(OnClickCloseButton);
        _popupButton.onClick.AddListener(OnClickCloseButton);
    }

    private void OnClickCloseButton()
    {
        Pop();
    }
}
