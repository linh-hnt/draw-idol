using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AFramework.UI;
using UnityEngine.UI;

public class LevelImagePopup : BaseUIMenu
{
    public static string Identifier = "Popup/LevelImagePopup";

    [SerializeField] private Image _levelImage;
    [SerializeField] private Button _closeButton;

    private void Awake()
    {
        if(_closeButton)
        {
            _closeButton.onClick.AddListener(OnClickCloseButton);
        }
    }

    public override void Init(object[] initParams)
    {
        base.Init(initParams);

        Sprite currentLevelSprite = (Sprite)initParams[0];

        _levelImage.sprite = currentLevelSprite;
    }

    private void OnClickCloseButton()
    {
        Pop();
    }
}
