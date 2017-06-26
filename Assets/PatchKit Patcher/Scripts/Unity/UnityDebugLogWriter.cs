﻿using PatchKit.Patcher.Debug;

namespace PatchKit.Patcher.Unity
{
    public class UnityDebugLogWriter : IDebugLogWriter
    {
        public void Log(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public void LogWarning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}