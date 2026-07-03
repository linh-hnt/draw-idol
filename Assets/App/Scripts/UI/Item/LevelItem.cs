using System.Collections;
using System.Collections.Generic;
using AFramework.UI;
using App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelItem : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _titleBackgroundImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [UnityEngine.SerializeField] private Image _backgroundImage;
    [SerializeField] private GameObject _lockObject;
    [SerializeField] private Button _itemButton;

    private LevelItemStatus _currentStatus = LevelItemStatus.Lock;
    private int _levelId = 0;
    private int _index = 0;
    private LevelInfo _levelInfor = null;

    private void Start()
    {
        _itemButton.onClick.AddListener(OnClickItemButton);
    }

    public void InitItem(int index, LevelInfo levelInfo, ChapterInfor chapterInfor)
    {
        _levelInfor = levelInfo;
        _levelId = levelInfo.id;
        _index = index;

        if (_nameText != null)
        {
            _nameText.SetText(levelInfo.levelName);

            var currentMaterial = GlobalData.I.GetUITextMterial(chapterInfor.levelItemData.textColorStatus);

            if (currentMaterial)
            {
                _nameText.fontMaterial = currentMaterial;
            }
        }

        if (_iconImage != null)
        {
            _iconImage.sprite = levelInfo.iconSprite;
        }

        if (_backgroundImage && chapterInfor != null)
        {
            _backgroundImage.sprite = chapterInfor.levelItemData.normalSprite;
        }

        if (_titleBackgroundImage && chapterInfor != null)
        {
            _titleBackgroundImage.sprite = chapterInfor.levelItemData.titleBackgroundSprite;
        }
    }

    public void SetStatus(LevelItemStatus status)
    {
        if (_lockObject)
        {
            _lockObject.SetActive(status == LevelItemStatus.Lock);
        }

        Material currentMaterial = null;
        switch (status)
        {
            case LevelItemStatus.Normal:
                {

                }
                break;
            case LevelItemStatus.Lock:
                {
                    currentMaterial = GlobalData.I.UIGrayMaterial;
                }
                break;
            default:
                break;
        }

        if (_backgroundImage)
        {
            _backgroundImage.material = currentMaterial;
        }

        if (_iconImage)
        {
            _iconImage.material = currentMaterial;
        }

        _currentStatus = status;
    }

    private void OnClickItemButton()
    {
        if (_currentStatus == LevelItemStatus.Lock)
        {
            return;
        }

        CanvasManager.PopAllLayer(eUILayer.Menu);
        CanvasManager.PopAllLayer(eUILayer.Popup);

        CanvasManager.Push(InGameMenu.Identifier, new object[] { _index, _levelInfor });
        GameplayManager.I.LoadLevel(_levelId);
        GameplayManager.I.SetChapterData(_levelInfor.chapter, _index);
    }
}

public enum LevelItemStatus
{
    Normal,
    Lock,
}
