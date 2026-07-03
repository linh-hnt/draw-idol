using System.Collections;
using System.Collections.Generic;
using AFramework.UI;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : BaseUIMenu
{
    public static string Identifier = "Menu/MainMenu";

    [Header("Chapter")]
    [SerializeField] private ScrollRect _chapterScrollRect;
    [SerializeField] private Transform _chapterContain;
    [UnityEngine.SerializeField] private ChapterItem _chapterItemPrefab;

    [Header("Button")]
    [SerializeField] private Button _settingButton;

    [Header("Cheat")]
    [SerializeField] private Button _cheatButton;

    private int _currentChapterCount = 0;

    private void Awake()
    {
#if USE_CHEAT
        _cheatButton.gameObject.SetActive(true);
#else
        _cheatButton.gameObject.SetActive(false);
#endif

        if (_cheatButton.gameObject.activeSelf)
        {
            _cheatButton.onClick.AddListener(() =>
            {
                CanvasManager.Push(CheatMenu.Identifier, null);
            });
        }

        _settingButton.onClick.AddListener(OnClickSettingButton);
    }

    public override void Init(object[] initParams)
    {
        base.Init(initParams);

        InitMenu();
    }

    private void InitMenu()
    {
        UpdateChapter();
    }

    private void OnClickSettingButton()
    {
        CanvasManager.Push(SettingPopup.Identifier, null);
    }

    private void UpdateChapter()
    {
        var chapterInfors = ConfigManager.I.ChapterConfigSO.GetChapterInfors();
        ChapterInfor chapterInfor = null;
        ChapterItem chapterItem = null;
        int begin = _currentChapterCount;

        for (int i = begin, length = chapterInfors.Count; i < length; i++)
        {
            chapterInfor = chapterInfors[i];

            chapterItem = Instantiate(_chapterItemPrefab, _chapterContain);
            chapterItem.InitItem(chapterInfor);
            _currentChapterCount++;
        }

        if (_chapterScrollRect != null)
        {
            _chapterScrollRect.content.anchoredPosition = Vector2.zero;
        }
    }
}
