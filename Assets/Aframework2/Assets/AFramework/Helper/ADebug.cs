using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public static class ADebug
{
    #region LOG_RELEASE
    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogRelease(object message, Object context)
    {
        UnityEngine.Debug.Log(message, context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogRelease(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogErrorRelease(object message)
    {
        UnityEngine.Debug.LogError(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogErrorRelease(object message, Object context)
    {
        UnityEngine.Debug.LogError(message, context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogErrorFormatRelease(string format, params object[] args)
    {
        UnityEngine.Debug.LogErrorFormat(format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogErrorFormatRelease(Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogErrorFormat(context, format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogExceptionRelease(System.Exception exception)
    {
        UnityEngine.Debug.LogException(exception);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogExceptionRelease(System.Exception exception, Object context)
    {
        UnityEngine.Debug.LogException(exception, context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogFormatRelease(Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(context, format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogFormatRelease(LogType logType, LogOption logOptions, Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(logType, logOptions, context, format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogFormatRelease(string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogWarningRelease(object message, Object context)
    {
        UnityEngine.Debug.LogWarning(message, context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogWarningRelease(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogWarningFormatRelease(Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogWarningFormat(context, format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD"), Conditional("LOG_RELEASE")]
    public static void LogWarningFormatRelease(string format, params object[] args)
    {
        UnityEngine.Debug.LogWarningFormat(format, args);
    }
    #endregion

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, Object context)
    {
        UnityEngine.Debug.Log(message, context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_ASSERTIONS")]
    public static void LogAssertion(object message, Object context)
    {
        UnityEngine.Debug.LogAssertion(message, context);
    }

    [Conditional("UNITY_ASSERTIONS")]
    public static void LogAssertion(object message)
    {
        UnityEngine.Debug.LogAssertion(message);
    }

    [Conditional("UNITY_ASSERTIONS")]
    public static void LogAssertionFormat(Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogAssertionFormat(context, format, args);
    }

    [Conditional("UNITY_ASSERTIONS")]
    public static void LogAssertionFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogAssertionFormat(format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message, Object context)
    {
        UnityEngine.Debug.LogError(message, context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogErrorFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogErrorFormat(format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogErrorFormat(Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogErrorFormat(context, format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogException(System.Exception exception)
    {
        UnityEngine.Debug.LogException(exception);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogException(System.Exception exception, Object context)
    {
        UnityEngine.Debug.LogException(exception, context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogFormat(Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(context, format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogFormat(LogType logType, LogOption logOptions, Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(logType, logOptions, context, format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogFormat(format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message, Object context)
    {
        UnityEngine.Debug.LogWarning(message, context);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarningFormat(Object context, string format, params object[] args)
    {
        UnityEngine.Debug.LogWarningFormat(context, format, args);
    }

    [Conditional("UNITY_EDITOR"), Conditional("USE_CHEAT"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarningFormat(string format, params object[] args)
    {
        UnityEngine.Debug.LogWarningFormat(format, args);
    }

    [Conditional("UNITY_EDITOR")]
    public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration)
    {
        UnityEngine.Debug.DrawLine(start, end, color, duration);
    }

    [Conditional("UNITY_EDITOR")]
    public static void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        UnityEngine.Debug.DrawLine(start, end, color);
    }

    [Conditional("UNITY_EDITOR")]
    public static void DrawLine(Vector3 start, Vector3 end)
    {
        UnityEngine.Debug.DrawLine(start, end);
    }

    [Conditional("UNITY_EDITOR")]
    public static void DrawRay(Vector3 start, Vector3 dir, Color color, float duration)
    {
        UnityEngine.Debug.DrawRay(start, dir, color, duration);
    }

    [Conditional("UNITY_EDITOR")]
    public static void DrawRay(Vector3 start, Vector3 dir, Color color)
    {
        UnityEngine.Debug.DrawRay(start, dir, color);
    }

    [Conditional("UNITY_EDITOR")]
    public static void DrawRay(Vector3 start, Vector3 dir)
    {
        UnityEngine.Debug.DrawRay(start, dir);
    }

    [Conditional("UNITY_EDITOR")]
    public static void DrawCircle(Vector3 position, float radius, int segments, Color color)
    {
        if (radius <= 0.0f || segments <= 0) return;
        float angleStep = 360.0f / segments;
        angleStep *= Mathf.Deg2Rad;
        Vector3 lineStart = Vector3.zero;
        Vector3 lineEnd = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            lineStart.x = Mathf.Cos(angleStep * i);
            lineStart.y = Mathf.Sin(angleStep * i);
            lineEnd.x = Mathf.Cos(angleStep * (i + 1));
            lineEnd.y = Mathf.Sin(angleStep * (i + 1));
            lineStart *= radius;
            lineEnd *= radius;
            lineStart += position;
            lineEnd += position;
            UnityEngine.Debug.DrawLine(lineStart, lineEnd, color);
        }
    }
}
