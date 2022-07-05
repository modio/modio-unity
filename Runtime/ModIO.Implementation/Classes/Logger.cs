using System.Collections.Generic;
using UnityEngine;

namespace ModIO.Implementation
{

    /// <summary>
    /// This class is responsible for outputting all of the logs that pertain to the ModIO Plugin
    /// </summary>
    internal static class Logger
    {
        internal const string ModioLogPrefix = "[mod.io]";

        static LogMessageDelegate LogDelegate = UnityLogDelegate;

        private static LogToPC _logToPC;
        internal static LogToPC LogToPC
        {
            get {
                return _logToPC = _logToPC ?? new LogToPC();
            }
        }

        internal static void SetLoggingDelegate(LogMessageDelegate loggingDelegate)
        {
            Logger.LogDelegate = loggingDelegate ?? UnityLogDelegate;
        }

        internal static void ResetLoggingDelegate()
        {
            Logger.LogDelegate = UnityLogDelegate;
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
                case LogLevel.Error:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Message:
                    Debug.Log(logMessage);
                    break;
                case LogLevel.Verbose:
                    Debug.Log(logMessage);
                    break;
            }
        }

        static bool IsThisLogAboveMaxLogLevelSetting(LogLevel level)
        {
            return (int)level > (int)Settings.build.logLevel;
        }

        internal static void Log(LogLevel logLevel, string logMessage, bool attemptLogToPc = true)
        {
            logMessage = $"{ModioLogPrefix} {logMessage}";
            Logger.LogDelegate?.Invoke(logLevel, logMessage);

#if UNITY_STANDALONE || UNITY_EDITOR
            if(attemptLogToPc)
            {
                try
                {
                    LogToPC.Log(logLevel, logMessage);
                }
                catch(System.Exception ex)
                {
                    Debug.LogError($"Error trying to write a message to pc log. Halting log to pc functionality for this session. Exception: {ex}.");
                    Logger.LogDelegate?.Invoke(logLevel, $"Error trying to write a message to pc log. Halting log to pc functionality for this session. Exception: {ex}.");
                    LogToPC.halt = true;
                }
            }            
#endif
        }
    }
}
