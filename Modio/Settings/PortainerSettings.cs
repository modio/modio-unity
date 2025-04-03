namespace Modio.Settings
{
    /// <summary>
    /// Supports mod.io's internal tests
    /// </summary>
    public class PortainerSettings : IModioServiceSettings
    {
        public string Stack;
        public LogLevel LogLevel = LogLevel.Warning;
    }
}
