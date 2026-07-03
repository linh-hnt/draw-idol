using System.Collections;
using System.Collections.Generic;
using AFramework.UI;
using App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameMenu : BaseUIMenu
{
    public static string Identifier = "Menu/InGameMenu";

    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _levelNameText;
    [SerializeField] private Image _levelIconImage;

    [Header("Select Color")]
    [SerializeField] private IngameSelectColor _ingameSelectColor;

    [Header("Button")]
    [SerializeField] private Button _levelImageButton;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _homeButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _skipButton;
    [SerializeField] private Button _settingButton;

    private Sprite _currentLevelSprite = null;

    private void Awake()
    {
        _closeButton.onClick.AddListener(OnClickCloseButton);
        _homeButton.onClick.AddListener(OnClickHomeButton);
        _resetButton.onClick.AddListener(OnClickResetButton);
        _skipButton.onClick.AddListener(OnClickSkipButton);
        _settingButton.onClick.AddListener(OnClickSettingButton);

        if (_levelImageButton)
            _levelImageButton.onClick.AddListener(OnClickLevelImageButton);

        EventManager.I.ShowIngameSelectColorAction += OnShowSelectColor;
        EventManager.I.HideIngameSelectColorAction += OnHideSelectColor;
    }

    private void OnEnable()
    {
        EventManager.I.NextLevelAction += OnNextLevel;
    }

    private void OnDisable()
    {
        EventManager.I.NextLevelAction -= OnNextLevel;
    }

    public override void Init(object[] initParams)
    {
        base.Init(initParams);

        int levelIndex = (int)initParams[0];
        LevelInfo levelInfo = (LevelInfo)initParams[1];

        OnHideSelectColor();

        InitMenu(levelIndex, levelInfo);
    }

    private void InitMenu(int levelIndex, LevelInfo levelInfo)
    {
        GameManager.I.CurrnetLevelIndex = levelIndex;

        string levelString = $"Level {levelIndex + 1}";
        _levelText.SetText(levelString);

        _levelNameText.SetText(levelInfo.levelName);
        _levelIconImage.sprite = levelInfo.iconSprite;

        _currentLevelSprite = levelInfo.iconSprite;
    }

    private void OnNextLevel(int levelIndex, LevelInfo levelInfo)
    {
        OnHideSelectColor();

        InitMenu(levelIndex, levelInfo);
    }

    private void OnClickCloseButton()
    {
        BackToMainMenu();

        CanvasManager.Push(SelectLevelPopup.Identifier, new object[] { GameplayManager.I.CurrentChapter });
    }

    private void OnClickHomeButton()
    {
        BackToMainMenu();
    }

    private void BackToMainMenu()
    {
        CanvasManager.PopAllLayer(eUILayer.Menu);
        CanvasManager.PopAllLayer(eUILayer.Popup);

        EventManager.I.CloseLevelAction?.Invoke();

        CanvasManager.Push(MainMenu.Identifier, null);
    }

    private void OnClickResetButton()
    {
        GameplayManager.I.CurrentLevelController?.SmartRestartStep();
    }

    private void OnClickSkipButton()
    {
        GameplayManager.I.CurrentLevelController?.WinLevel();
    }

    private void OnClickLevelImageButton()
    {
        CanvasManager.Push(LevelImagePopup.Identifier, new object[] { _currentLevelSprite });
    }

    private void OnClickSettingButton()
    {
        CanvasManager.Push(SettingPopup.Identifier, null);
    }

    private void OnShowSelectColor(ColorContent colorContent, bool hasShowFinishPaint)
    {
        if (_ingameSelectColor)
        {
            _ingameSelectColor.SetShowSelectColor(colorContent, hasShowFinishPaint);
        }
    }

    private void OnHideSelectColor()
    {
        if (_ingameSelectColor)
        {
            _ingameSelectColor.HideSelectColor();
        }
    }
}
