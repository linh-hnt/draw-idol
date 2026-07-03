using System.Collections;
using System.Collections.Generic;
using AFramework;
using AFramework.UI;
using App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheatMenu : BaseUIMenu
{
    public static string Identifier = "Menu/CheatMenu";

    [SerializeField] private TMP_Dropdown _selectChapterDropdown;
    [SerializeField] private TMP_InputField _levelInputField;
    [SerializeField] private Button _unlockAllLevelButton;
    [SerializeField] private Button _closeButton;

    private ChapterName _chapter = ChapterName.BrainBrot;

    private void UpdateWhenAwake()
    {
        for (int i = 0, length = (int)ChapterName.NUM; i < length; i++)
        {
            _selectChapterDropdown.options.Add(new TMP_Dropdown.OptionData(((ChapterName)i).ToString()));
        }
    }

    private void Awake()
    {
        UpdateWhenAwake();

        _selectChapterDropdown.onValueChanged.AddListener(value =>
        {
            _chapter = (ChapterName)value;
        });

        _levelInputField.onValueChanged.AddListener(valueString =>
        {
            int levelView = 0;
            bool convertSuccess = int.TryParse(valueString, out levelView);
            if (convertSuccess)
            {
                OnSetLevel(levelView);
            }
        });

        _unlockAllLevelButton.onClick.AddListener(OnClickUnlockAllLevelButton);
        _closeButton.onClick.AddListener(OnClickCloseButton);
    }

    private void OnSetLevel(int levelView)
    {
        if (levelView <= 0)
        {
            return;
        }

        int levelIndex = levelView - 1;
        SaveManager.I.SetChapterLevelIndex(_chapter, levelIndex);
    }

    private void OnClickUnlockAllLevelButton()
    {
        var levelConfig = ConfigManager.I.LevelConfigSO;

        int maxLevelIndex = 0;
        ChapterName chapter = ChapterName.BrainBrot;

        for (int i = 0, length = (int)ChapterName.NUM; i < length; i++)
        {
            chapter = (ChapterName)i;

            maxLevelIndex = levelConfig.GetTotalLevel(chapter);
            maxLevelIndex = Mathf.Max(0, maxLevelIndex);
            SaveManager.I.SetChapterLevelIndex(chapter, maxLevelIndex);
        }

    }

    private void OnClickCloseButton()
    {
        Pop();
    }
}
