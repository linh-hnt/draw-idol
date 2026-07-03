using System.Collections;
using System.Collections.Generic;
using AFramework;
using App;
using UnityEngine;

public class ConfigManager : ManualSingletonMono<ConfigManager>
{
    [SerializeField] private LevelConfigSO _levelConfigSO;
    [SerializeField] private ChapterConfigSO _chapterConfigSO;

    public LevelConfigSO LevelConfigSO => _levelConfigSO;
    public ChapterConfigSO ChapterConfigSO => _chapterConfigSO;
}
