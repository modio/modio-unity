using System;

namespace Modio.Settings
{
    [Serializable]
    public class TempModInstallationSettings : IModioServiceSettings
    {
        // 0 means the mods will be removed on a session end.
        public int LifeTimeDays = 0;
    }
}
