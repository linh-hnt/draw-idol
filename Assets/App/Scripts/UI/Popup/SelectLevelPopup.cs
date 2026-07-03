using System.Collections;
using System.Collections.Generic;
using AFramework.UI;
using App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectLevelPopup : BaseUIMenu
{
    public static string Identifier = "Popup/SelectLevelPopup";

    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Transform _levelContain;
    [SerializeField] private LevelItem _levelItemPrefab;
    [SerializeField] private TextMeshProUGUI _titleNameText;
    [SerializeField] private List<Image> _titleBackgroundImages;

    [Header("Button")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _settingButton;

    private List<LevelItem> _levelItems = new List<LevelItem>();
    private ChapterName _currentChapter = ChapterName.BrainBrot;

    private void Awake()
    {
        _closeButton.onClick.AddListener(OnClickCloseButton);

        _settingButton.onClick.AddListener(OnClickSettingButton);
    }

    public override void Init(object[] initParams)
    {
        base.Init(initParams);

        _currentChapter = (ChapterName)initParams[0];

        UpdateLevelFollowChapter(_currentChapter);

        if (_scrollRect)
        {
            _scrollRect.content.anchoredPosition = Vector2.zero;
        }
    }

    private void UpdateLevelFollowChapter(ChapterName chapterName)
    {
        ChapterInfor chapterInfor = ConfigManager.I.ChapterConfigSO.GetChapterInfor(chapterName);
        if (chapterInfor != null)
        {
            if (_titleBackgroundImages != null && _titleBackgroundImages.Count <= 0)
            {
                for (int i = 0, length = _titleBackgroundImages.Count; i < length; i++)
                {
                    if (_titleBackgroundImages[i] != null)
                        _titleBackgroundImages[i].sprite = chapterInfor.titleBackgroundSprite;
                }
            }

            if (_titleNameText != null)
            {
                _titleNameText.SetText(chapterInfor.chapterName);
            }
        }

        var levelInfors = ConfigManager.I.LevelConfigSO.GetLevelsByChapter(chapterName);

        int itemsCount = _levelItems.Count;
        LevelItem currentLevelItem = null;

        for (int i = 0, length = levelInfors.Count; i < length; i++)
        {
            currentLevelItem = Instantiate(_levelItemPrefab, _levelContain);
            _levelItems.Add(currentLevelItem);
        }

        int inforsCount = levelInfors.Count;
        LevelInfo currentLevelInfor = null;
        LevelItemStatus levelItemStatus = LevelItemStatus.Normal;
        for (int i = 0, length = _levelItems.Count; i < length; i++)
        {
            currentLevelItem = _levelItems[i];

            currentLevelItem.gameObject.SetActive(i < inforsCount);

            if (i < inforsCount)
            {
                currentLevelInfor = levelInfors[i];
                currentLevelItem.InitItem(i, currentLevelInfor, chapterInfor);

                int currentLevelIndex = SaveManager.I.GetChapterCurrentLevelIndex(chapterName);
                levelItemStatus = i <= currentLevelIndex ? LevelItemStatus.Normal : LevelItemStatus.Lock;
                currentLevelItem.SetStatus(levelItemStatus);
            }
        }
    }

    private void OnClickCloseButton()
    {
        Pop();
    }

    private void OnClickSettingButton()
    {
        CanvasManager.Push(SettingPopup.Identifier, null);
    }
}
