using System;

namespace Modio.Settings
{
    [Serializable]
    public class ModInstallationManagementSettings : IModioServiceSettings
    {
        public bool AutoActivate = true;
    }
}
