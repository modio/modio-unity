#if UNITY_2019_4_OR_NEWER
using UnityEngine;
#else
using System;
#endif

namespace ModIO.Implementation
{

    /// <summary>
    /// This class is responsible for outputting all of the logs that pertain to the ModIO Plugin
    /// </summary>
    public static class Logger
    {
        internal const string ModioLogPrefix = "[mod.io]";

        static LogMessageDelegate LogDelegate = UnityLogDelegate;

        internal static void SetLoggingDelegate(LogMessageDelegate loggingDelegate)
        {
            LogDelegate = loggingDelegate ?? UnityLogDelegate;
        }

        internal static void ResetLoggingDelegate()
        {
            LogDelegate = UnityLogDelegate;
        }

        internal static void UnityLogDelegate(LogLevel logLevel, string logMessage)
        {
            // check the log level
            if(IsThisLogAboveMaxLogLevelSetting(logLevel))
            {
                return;
            }

            switch(logLevel)
            {
#if UNITY_2019_4_OR_NEWER
                case LogLevel.Error:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Message:
                case LogLevel.Verbose:
                    Debug.Log(logMessage);
                    break;
#else
                case LogLevel.Error:
                    Console.WriteLine($"[ERROR] {logMessage}");
                    break;
                case LogLevel.Warning:
                    Console.WriteLine($"[WARNING] {logMessage}");
                    break;
                case LogLevel.Message:
                case LogLevel.Verbose:
                    Console.WriteLine(logMessage);
                    break;
#endif
            }
        }

        static bool IsThisLogAboveMaxLogLevelSetting(LogLevel level)
        {
            if (Settings.build != null)
            {
                return (int)level > (int)Settings.build.logLevel;
            }

            return true;
        }

        public static void Log(LogLevel logLevel, string logMessage)
        {
            logMessage = $"{ModioLogPrefix} {logMessage}";
            LogDelegate?.Invoke(logLevel, logMessage);
        }
    }
}
