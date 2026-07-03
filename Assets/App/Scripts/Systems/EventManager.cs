using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AFramework;
using System;
using App;

public class EventManager : ManualSingletonMono<EventManager>
{
    public Action<Color, int> SetPaintColorAction;
    public Action<Color> SetColorPickerAction;
    public Action FinishPaintAction;
    public Action CloseLevelAction;
    public Action CompleteLevelAction;

    public Action<ColorContent, bool> ShowIngameSelectColorAction;
    public Action HideIngameSelectColorAction;

    public Action<int, LevelInfo> NextLevelAction;
}
