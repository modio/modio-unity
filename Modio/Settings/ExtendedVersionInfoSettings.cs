using System;

namespace Modio.Settings
{
    /// <summary>
    /// Used to show additional version info (e.g. build number) on the mod.io UI
    /// </summary>
    [Serializable]
    public class ExtendedVersionInfoSettings : IModioServiceSettings
    {
        public string Info;
    }
}
