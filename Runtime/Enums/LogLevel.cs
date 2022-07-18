namespace ModIO
{
    /// <summary>
    /// The logging level of the plugin. Used in BuildSettings to determine which log messages to
    /// ignore or display.
    /// </summary>
    public enum LogLevel
    {
        None = -1,
        Error = 0,
        Warning = 1,
        Message = 2,
        Verbose = 3,
    }
}
