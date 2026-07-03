using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingItem : MonoBehaviour
{
    [SerializeField] private SettingType _settingType;
    [SerializeField] private GameObject _onObject;
    [SerializeField] private GameObject _offObject;
    [SerializeField] private Button _itemButton;

    private void Start()
    {
        if (_itemButton)
        {
            _itemButton.onClick.AddListener(OnClickItemButton);
        }

        UpdateUI();
    }

    private void OnClickItemButton()
    {
        bool currentValue = SaveManager.I.GetSettingValue(_settingType);

        currentValue = !currentValue;
        SaveManager.I.SetSettingValue(_settingType, currentValue);

        _onObject.SetActive(currentValue);
        _offObject.SetActive(!currentValue);
    }

    private void UpdateUI()
    {
        bool currentValue = SaveManager.I.GetSettingValue(_settingType);
        _onObject.SetActive(currentValue);
        _offObject.SetActive(!currentValue);
    }
}

public enum SettingType
{
    Sound,
    Music,
    Vibration,
}
