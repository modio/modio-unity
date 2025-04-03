using UnityEngine;

namespace Modio.Unity
{
    public class ModioUnityLogger : IModioLogHandler
    {
        readonly string _prefix;

        public ModioUnityLogger() : this(ModioLog.LOG_PREFIX_DEFAULT)
        {
        }

        public ModioUnityLogger(string prefix) => _prefix = prefix;

        public void LogHandler(LogLevel logLevel, object message)
        {
            string logLevelPrefix = logLevel switch
            {
                LogLevel.Error   => "[ERROR] ",
                LogLevel.Warning => "[WARNING] ",
                _                => string.Empty,
            };
            Debug.unityLogger.Log(
                logLevel switch
                {
                    LogLevel.None    => LogType.Log,
                    LogLevel.Error   => LogType.Error,
                    LogLevel.Warning => LogType.Warning,
                    LogLevel.Message => LogType.Log,
                    LogLevel.Verbose => LogType.Log,
                    _                => LogType.Log,
                }, $"{_prefix}{logLevelPrefix}{message}"
            );
        }
    }
}
