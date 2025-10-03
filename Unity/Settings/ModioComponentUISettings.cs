using System;
using UnityEngine;

namespace Modio.Unity.Settings
{
    [Serializable]
    public class ModioComponentUISettings : IModioServiceSettings
    {
        public bool ShowMonetizationUI;
        public bool ShowEnableModToggle;
        public bool FallbackToEmailAuthentication;
        [Tooltip("Enable selecting auth method to use at run-time. Will not be available on consoles or mobile.")]
        public bool EnableAuthSelection;
    }
}
