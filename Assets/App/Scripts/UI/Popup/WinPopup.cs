using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AFramework.UI;
using App;
using TMPro;

public class WinPopup : BaseUIMenu
{
    public static string Identifier = "Popup/WinPopup";

    [SerializeField] private Button _backToHomeButton;
    [SerializeField] private Button _nextLevelButton;
    [SerializeField] private RectTransform _backToHomeButtonRect;
    [SerializeField] private Image _levelIconImage;
    [SerializeField] private RectTransform _progressBarRect;
    [SerializeField] private TextMeshProUGUI _progressText;

    private float _maxProgressWidth;

    private void Awake()
    {
        _maxProgressWidth = _progressBarRect.rect.width;
        _backToHomeButton.onClick.AddListener(OnClickBackToHome);
        _nextLevelButton.onClick.AddListener(OnClickNextLevel);
    }

    public override void Init(object[] initParams)
    {
        base.Init(initParams);

        SetupLevelIcon();
        UpdateNextLevelButtonVisibility();
        SetProgress(GameplayManager.I.ProgressLevel);
    }

    private void OnClickBackToHome()
    {
        GameManager.I.DoVibration();

        CanvasManager.PopAllLayer(eUILayer.Menu);
        CanvasManager.PopAllLayer(eUILayer.Popup);

        EventManager.I.CloseLevelAction?.Invoke();

        CanvasManager.Push(MainMenu.Identifier, null);
        CanvasManager.Push(SelectLevelPopup.Identifier, new object[] { GameplayManager.I.CurrentChapter });
    }

    private void OnClickNextLevel()
    {
        GameManager.I.DoVibration();

        var currentChapter = GameplayManager.I.CurrentChapter;
        var currentChapterIndex = GameplayManager.I.CurrentChapterIndex;
        var levels = ConfigManager.I.LevelConfigSO.GetLevelsByChapter(currentChapter);

        int nextLevelId;

        int nextLevelIndex = 0;

        if (currentChapterIndex + 1 < levels.Count)
        {
            nextLevelId = levels[currentChapterIndex + 1].id;
            GameplayManager.I.SetChapterData(currentChapter, currentChapterIndex + 1);

            nextLevelIndex = GameManager.I.CurrnetLevelIndex + 1;
        }
        else
        {
            var chapters = ConfigManager.I.ChapterConfigSO.GetChapterInfors();
            int chapterIndex = chapters.FindIndex(c => c.chapter == currentChapter);
            var nextChapter = chapters[chapterIndex + 1].chapter;
            var nextChapterLevels = ConfigManager.I.LevelConfigSO.GetLevelsByChapter(nextChapter);

            nextLevelId = nextChapterLevels[0].id;
            GameplayManager.I.SetChapterData(nextChapter, 0);
        }

        CanvasManager.PopAllLayer(eUILayer.Popup);

        GameplayManager.I.LoadLevel(nextLevelId);

        LevelInfo nextLevelInfor = ConfigManager.I.LevelConfigSO.GetLevelById(nextLevelId);


        EventManager.I.NextLevelAction?.Invoke(nextLevelIndex, nextLevelInfor);
    }

    private void SetupLevelIcon()
    {
        var currentChapter = GameplayManager.I.CurrentChapter;
        var currentIndex = GameplayManager.I.CurrentChapterIndex;
        var levels = ConfigManager.I.LevelConfigSO.GetLevelsByChapter(currentChapter);

        if (currentIndex >= 0 && currentIndex < levels.Count)
        {
            _levelIconImage.sprite = levels[currentIndex].iconSprite;
        }
    }

    private void UpdateNextLevelButtonVisibility()
    {
        var currentChapter = GameplayManager.I.CurrentChapter;
        var currentIndex = GameplayManager.I.CurrentChapterIndex;
        var levels = ConfigManager.I.LevelConfigSO.GetLevelsByChapter(currentChapter);

        bool isLastLevelInChapter = currentIndex >= levels.Count - 1;

        if (!isLastLevelInChapter)
        {
            _nextLevelButton.gameObject.SetActive(true);
            _backToHomeButtonRect.anchoredPosition = new Vector2(-165f, 129f);
            _backToHomeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 145f);
            return;
        }

        var chapters = ConfigManager.I.ChapterConfigSO.GetChapterInfors();
        int chapterIndex = chapters.FindIndex(c => c.chapter == currentChapter);

        bool isLastChapter = chapterIndex >= chapters.Count - 1;
        _nextLevelButton.gameObject.SetActive(!isLastChapter);
        _backToHomeButtonRect.anchoredPosition = isLastChapter ? new Vector2(0, 129f) : new Vector2(-165f, 129f);
        _backToHomeButtonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, isLastChapter ? 320 : 145);
    }

    public void SetProgress(float percent)
    {
        percent = Mathf.Clamp01(percent);
        float targetWidth = _maxProgressWidth * percent;
        _progressBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);

        if (_progressText)
        {
            int currentPercent = (int)(percent * 100f);
            _progressText.SetText($"{currentPercent}%");
        }
    }
}
