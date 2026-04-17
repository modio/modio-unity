using System;
using System.Linq;

namespace Modio.Unity
{
    public static class ModioUnitySettingsExtensions
    {
        public static T GetOrAddPlatformSetting<T>(this ModioUnitySettings unitySettings) where T : IModioServiceSettings
        {
            if (unitySettings.Settings.TryGetPlatformSettings(out T superType))
                return superType;

            var newSettingsObject = Activator.CreateInstance<T>();
                
            IModioServiceSettings[] newPlatformSettings =
                unitySettings.Settings.PlatformSettings.Append(newSettingsObject)
                             .ToArray();

            unitySettings.SetPlatformSettings(newPlatformSettings);

            return newSettingsObject;
        }
    }
}
