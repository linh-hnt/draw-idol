using System.Collections;
using System.Collections.Generic;
using AFramework.ExtensionMethods;
using AFramework.UI;
using App;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterItem : MonoBehaviour
{
    [SerializeField] private RectTransform _iconContainer;
    [SerializeField] private Image _buttonImage;
    [SerializeField] private TextMeshProUGUI _chapterNameText;
    [SerializeField] private Button _itemButton;

    private ChapterName chapterName = ChapterName.BrainBrot;

    private void Start()
    {
        _itemButton.onClick.AddListener(OnClickItemButton);
    }

    public void InitItem(ChapterInfor chapterInfor)
    {
        if (_iconContainer != null)
        {
            _iconContainer.DeleteAllChilds();
            Instantiate(chapterInfor.iconPrefab, _iconContainer);
        }

        if (_buttonImage != null)
        {
            _buttonImage.sprite = chapterInfor.buttonChapterSprite;
        }

        this.chapterName = chapterInfor.chapter;
        _chapterNameText.SetText(chapterInfor.chapterName);

        var currentFontMaterial = GlobalData.I.GetUITextMterial(chapterInfor.levelItemData.textColorStatus);
        _chapterNameText.fontMaterial = currentFontMaterial;
    }

    private void OnClickItemButton()
    {
        CanvasManager.Push(SelectLevelPopup.Identifier, new object[] { chapterName });
    }
}
