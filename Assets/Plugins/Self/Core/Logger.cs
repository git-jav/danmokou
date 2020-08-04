﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class Log {
    public enum Level {
        DEBUG1 = 1,
        DEBUG2 = 2,
        DEBUG3 = 3,
        DEBUG4 = 4,
        INFO = 5,
        WARNING = 6,
        ERROR = 7
    }

    private const int MIN_LEVEL = (int) Level.DEBUG2;
    private const int BUILD_MIN = (int) Level.INFO;

    public static void Unity(string msg, bool stackTrace = true, Level level = Level.INFO) {
        msg = $"Frame {ETime.FrameNumber}: {msg}";
        if ((int) level < MIN_LEVEL) return;
#if UNITY_EDITOR
#else
        if ((int) level < BUILD_MIN) return;
#endif
        LogOption lo = stackTrace ? LogOption.None : LogOption.NoStacktrace;
        LogType unityLT = LogType.Log;
        if (level == Level.WARNING) unityLT = LogType.Warning;
        if (level == Level.ERROR) {
            unityLT = LogType.Error;
            Debug.LogError(msg);
        } else {
            Debug.LogFormat(unityLT, lo, null, msg.Replace("{", "{{").Replace("}", "}}"));
        }
    }

    public static void UnityError(string msg) {
        Unity(msg, true, Level.ERROR);
    }

    /// <summary>
    /// Create an exception with all the InnerException messages combined.
    /// </summary>
    /// <param name="e"></param>
    /// <exception cref="Exception"></exception>
    public static Exception StackInnerException(Exception e) {
        StringBuilder msg = new StringBuilder();
        while (e != null) {
            msg.Append(e.Message);
            msg.Append("\n");
            e = e.InnerException;
        }
        return new Exception(msg.ToString());
    }
}