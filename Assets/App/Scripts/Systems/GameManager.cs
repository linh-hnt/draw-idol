using System.Collections;
using System.Collections.Generic;
using AFramework;
using AFramework.UI;
using UnityEngine;

public class GameManager : ManualSingletonMono<GameManager>
{
    private int _currentLevelIndex = -1;

    public int CurrnetLevelIndex
    {
        get => _currentLevelIndex;
        set
        {
            _currentLevelIndex = value;
        }
    }

    void Start()
    {
        BaseUIMenu mainMenu = CanvasManager.Init("UI/", MainMenu.Identifier);

        SaveManager.I.RegisterSaveData();

        SaveGameManager.I.Load();
    }

    public void DoVibration()
    {
#if !UNITY_EDITOR
        bool status = SaveManager.I.GetSettingValue(SettingType.Vibration);
        if (status)
        {
            MoreMountains.NiceVibrations.MMVibrationManager.Haptic(MoreMountains.NiceVibrations.HapticTypes.LightImpact);
        }  
#endif
    }
}
