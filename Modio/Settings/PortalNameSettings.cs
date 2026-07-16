using System;
using System.Linq;
using Modio.API;

namespace Modio.Settings
{
    [Serializable]
    public class PortalNameSettings : IModioServiceSettings
    {
        public ModioAPI.Portal[] _ignorePortalNameOn;
        public ModioAPI.Portal[] _ignorePortalAvatarOn;
        
        bool _hasLoggedError;

        public bool ShouldIgnorePortalNameOn(ModioAPI.Portal portal)
        {
            bool shouldIgnore = _ignorePortalNameOn.Contains(portal);
            if (shouldIgnore)
                CheckWarnings(portal);
            return shouldIgnore;
        }

        public bool ShouldIgnorePortalAvatarOn(ModioAPI.Portal portal)
        {
            bool shouldIgnore = _ignorePortalAvatarOn.Contains(portal);
            if (shouldIgnore)
                CheckWarnings(portal);
            return shouldIgnore;
        }

        void CheckWarnings(ModioAPI.Portal portal)
        {
            if (!_hasLoggedError && portal is ModioAPI.Portal.PlayStationNetwork or ModioAPI.Portal.XboxLive or ModioAPI.Portal.Nintendo)
            {
                _hasLoggedError = true;
                ModioLog.Error?.Log($"Disabling portal names on {portal} is strongly discouraged. Check individual platform documentation for further info.");
            }
        }
    }
}
