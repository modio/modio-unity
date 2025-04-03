using System;

namespace Modio
{
    public class ModioConsoleLog : IModioLogHandler {
        readonly string _logPrefix = ModioLog.LOG_PREFIX_DEFAULT;

        public ModioConsoleLog() : this(ModioLog.LOG_PREFIX_DEFAULT)
        {
        }

        public ModioConsoleLog(string logPrefix) => _logPrefix = logPrefix;

        public void LogHandler(LogLevel logLevel, object message)
        {
            string logLevelPrefix = logLevel switch
            {
                LogLevel.Error   => "[ERROR] ",
                LogLevel.Warning => "[WARNING] ",
                _                => string.Empty,
            };
            Console.WriteLine($"{_logPrefix}{logLevelPrefix}{logLevelPrefix}{message}");
        }
    }
}
