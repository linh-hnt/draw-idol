using System.Collections;
using System.Collections.Generic;
using App;
using UnityEngine;

[CreateAssetMenu(fileName = "ChapterConfigSO", menuName = "App/ChapterConfigSO")]
public class ChapterConfigSO : ScriptableObject
{
    [UnityEngine.SerializeField] private List<ChapterInfor> _chapterInfors = new List<ChapterInfor>();

    public int ChapterInforsLength
    {
        get
        {
            return _chapterInfors.Count;
        }
    }

    public List<ChapterInfor> GetChapterInfors()
    {
        return _chapterInfors;
    }

    public ChapterInfor GetChapterInfor(ChapterName chapterName)
    {
        return _chapterInfors.Find(infor => infor.chapter == chapterName);
    }
}

[System.Serializable]
public class ChapterInfor
{
    public ChapterName chapter = ChapterName.BrainBrot;
    public string chapterName = string.Empty;
    public GameObject iconPrefab;
    public Sprite buttonChapterSprite;
    public Sprite titleBackgroundSprite;

    public LevelItemData levelItemData;
}

[System.Serializable]
public class LevelItemData
{
    public Sprite normalSprite;
    public Sprite titleBackgroundSprite;
    public TextColorStatus textColorStatus;
}