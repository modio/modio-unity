using System;

namespace Modio.Settings
{
    /// <summary>
    /// Supports mod.io's internal tests
    /// </summary>
    [Serializable]
    public class PortainerSettings : IModioServiceSettings
    {
        public string Stack;
        public bool AlwaysReseed;
    }
}
