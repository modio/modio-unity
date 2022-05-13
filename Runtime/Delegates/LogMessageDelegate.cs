namespace ModIO
{
    /// <summary>Logging delegate that can be assigned via
    /// ModIOUnity.SetLogMessageDelegate.</summary>
    public delegate void LogMessageDelegate(LogLevel logLevel, string logMessage);
}
