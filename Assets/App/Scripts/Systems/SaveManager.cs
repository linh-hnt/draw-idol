using System.Collections;
using System.Collections.Generic;
using System.Threading;
using AFramework;
using App;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SaveManager : ManualSingletonMono<SaveManager>, ISaveData
{
    private SaveData _saveData;

    private CancellationTokenSource _cancellationTokenSource = null;

    private bool _dataChanged = false;
    public bool DataChanged
    {
        get
        {
            return _dataChanged;
        }
        set
        {
            _dataChanged = value;

            if (_dataChanged)
            {
                StopAllAsync();

                _cancellationTokenSource = new CancellationTokenSource();

                AsyncSaveData(_cancellationTokenSource.Token).Forget();
            }
        }
    }

    public object GetData()
    {
        return _saveData;
    }

    public void OnAllDataLoaded()
    {

    }

    public void RegisterSaveData()
    {
        AFramework.SaveGameManager.I.RegisterMandatoryData("SaveGameData", this as AFramework.ISaveData);
    }

    public void SetData(string data)
    {
        if (data == "")
        {
            _saveData = new SaveData();
        }
        else
        {
            _saveData = UnityEngine.JsonUtility.FromJson<SaveData>(data);
        }

        if (_saveData.chapterDatas.Count <= 0 || _saveData.chapterDatas.Count > (int)ChapterName.NUM)
        {
            int beginIndex = _saveData.chapterDatas.Count;
            for (int i = beginIndex, length = (int)ChapterName.NUM; i < length; i++)
            {
                _saveData.chapterDatas.Add(new ChapterData()
                {
                    chapterName = (ChapterName)i
                });
            }
        }

        DataChanged = true;
    }

    public int GetChapterCurrentLevelIndex(ChapterName chapterName)
    {
        ChapterData chapterData = _saveData.chapterDatas.Find(data => data.chapterName == chapterName);

        if (chapterData == null)
        {
            chapterData = new ChapterData()
            {
                chapterName = chapterName
            };
            _saveData.chapterDatas.Add(chapterData);

            DataChanged = true;
        }

        return chapterData.currentLevelIndex;
    }

    public int GetChapterCurrentLevelView(ChapterName chapterName)
    {
        return GetChapterCurrentLevelIndex(chapterName) + 1;
    }

    public void SetChapterLevelIndex(ChapterName chapterName, int levelIndex)
    {
        ChapterData chapterData = _saveData.chapterDatas.Find(data => data.chapterName == chapterName);

        if (chapterData == null)
        {
            chapterData = new ChapterData()
            {
                chapterName = chapterName,
                currentLevelIndex = levelIndex
            };

            _saveData.chapterDatas.Add(chapterData);
        }
        else
        {
            chapterData.currentLevelIndex = levelIndex;
        }

        DataChanged = true;
    }

    private async UniTask AsyncSaveData(CancellationToken cancellationToken)
    {
        await UniTask.DelayFrame(1, cancellationToken: cancellationToken);

        SaveGameManager.I.Save();
    }

    private void StopAllAsync()
    {
        if (_cancellationTokenSource == null)
            return;

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }

    public bool GetSettingValue(SettingType settingType)
    {
        bool returnValue = settingType switch
        {
            SettingType.Sound => _saveData.hasSound,
            SettingType.Music => _saveData.hasMusic,
            SettingType.Vibration => _saveData.hasVibration,
            _ => false
        };

        return returnValue;
    }

    public void SetSettingValue(SettingType settingType, bool setValue)
    {
        switch (settingType)
        {
            case SettingType.Sound:
                {
                    _saveData.hasSound = setValue;
                }
                break;
            case SettingType.Music:
                {
                    _saveData.hasMusic = setValue;
                }
                break;
            case SettingType.Vibration:
                {
                    _saveData.hasVibration = setValue;
                }
                break;
            default:
                break;
        }

        DataChanged = true;
    }
}

[System.Serializable]
public class SaveData
{
    public List<ChapterData> chapterDatas = new List<ChapterData>();

    public bool hasVibration = true;
    public bool hasSound = true;
    public bool hasMusic = true;
}

[System.Serializable]
public class ChapterData
{
    public ChapterName chapterName;
    public int currentLevelIndex;
}
