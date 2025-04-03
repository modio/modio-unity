using System;

namespace Modio.Unity.Settings
{
    [Serializable]
    public class ModioComponentUISettings : IModioServiceSettings
    {
        public bool ShowMonetizationUI;
        public bool ShowEnableModToggle;
        public bool FallbackToEmailAuthentication;
    }
}
