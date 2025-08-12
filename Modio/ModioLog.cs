using System;

namespace Modio
{
    public enum LogLevel : byte
    {
        None,
        Error,
        Warning,
        Message,
        Verbose,
    }

    public interface IModioLogHandler
    {
        void LogHandler(LogLevel logLevel, object message);
    }

    public class ModioLog
    {
        public delegate void LogHandler(LogLevel logLevel, object message);

        public const string LOG_PREFIX_DEFAULT = "[mod.io] ";

#nullable enable
        public static ModioLog? Error   { get; private set; }
        public static ModioLog? Warning { get; private set; }
        public static ModioLog? Message { get; private set; }
        public static ModioLog? Verbose { get; private set; }
#nullable disable

        static IModioLogHandler _logHandler;

        readonly LogLevel _logLevel;

        static ModioLog()
        {
            ApplyLogLevel(LogLevel.Verbose);

            ModioServices.Bind<IModioLogHandler>()
                         .FromNew<ModioConsoleLog>(ModioServicePriority.Default);
            ModioServices.AddBindingChangedListener<IModioLogHandler>(UpdateLogHandler);

            if (ModioCommandLine.TryGetArgument("loglevel", out string logLevelText))
            {
                if (Enum.TryParse(logLevelText, true, out LogLevel logLevelEnum))
                    ApplyLogLevel(logLevelEnum);
                else
                    // ReSharper disable once ExpressionIsAlwaysNull (it's set in ApplyLogLevel)
                    // ReSharper disable once ConstantConditionalAccessQualifier
                    Error?.Log($"Unrecognized log level: {logLevelText}");
            }
            else
                ModioServices.AddBindingChangedListener<ModioSettings>(GetLogLevelFromSettings);
        }

        static void UpdateLogHandler(IModioLogHandler logHandler) => _logHandler = logHandler;
        
        static void GetLogLevelFromSettings(ModioSettings settings) => ApplyLogLevel(settings.LogLevel);
        
        static void ApplyLogLevel(LogLevel logLevel)
        {
            Error   = logLevel < LogLevel.Error   ? null : Error   ?? new ModioLog(LogLevel.Error);
            Warning = logLevel < LogLevel.Warning ? null : Warning ?? new ModioLog(LogLevel.Warning);
            Message = logLevel < LogLevel.Message ? null : Message ?? new ModioLog(LogLevel.Message);
            Verbose = logLevel < LogLevel.Verbose ? null : Verbose ?? new ModioLog(LogLevel.Verbose);
        }

        ModioLog(LogLevel logLevel) => _logLevel = logLevel;

        public void Log(object message)
        {
            if (_logHandler != null)
                _logHandler.LogHandler(_logLevel, message);
            else
                Console.WriteLine($"{LOG_PREFIX_DEFAULT}{_logLevel}: {message}");
        }

#nullable enable
        public static ModioLog? GetLogLevel(LogLevel logLevel) => logLevel switch
#nullable disable
        {
            LogLevel.Error   => Error,
            LogLevel.Warning => Warning,
            LogLevel.Message => Message,
            LogLevel.Verbose => Verbose,
            _                     => Error,
        };
    }
}
