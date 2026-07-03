using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSDisplay : MonoBehaviour
{
    private const int Size = 80;

    public Color textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    private readonly string format = "{0:0.0} ms ({1:0.} fps)";
    private float _deltaTime;
    private int _w;
    private int _h;
    private GUIStyle _style;
    private Rect _rect;

    private void Start()
    {
        _w = Screen.width;
        _h = Screen.height;
        _style = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = _h * 2 / Size,
            normal =
            {
                textColor = textColor
            }
        };
        _rect = new Rect(0, 0, _w, _h * 2f / Size);
    }

    void Update()
    {
        _deltaTime += (Time.deltaTime - _deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        float msec = _deltaTime * 1000.0f;
        float fps = 1.0f / _deltaTime;
        GUI.Label(_rect, string.Format(format, msec, fps), _style);
    }
}