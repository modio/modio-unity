using System;
using Modio.API;

namespace Modio
{
    [Serializable]
    public class ModioSettings
    {
        public static event Action<ModioSettings> OnSettingsUpdated;
        
        public long GameId;
        public string APIKey;
        public string ServerURL;
        public string DefaultLanguage = "en";
        public LogLevel LogLevel = LogLevel.Warning;

        //Note this is not serializable; you must assign it yourself
        public IModioServiceSettings[] PlatformSettings;

        public T GetPlatformSettings<T>() where T : IModioServiceSettings
        {
            if (PlatformSettings == null) return default(T);

            foreach (IModioServiceSettings platform in PlatformSettings)
                if (platform is T t)
                    return t;

            return default(T);
        }

        public bool TryGetPlatformSettings<T>(out T settings) where T : IModioServiceSettings
        {
            if (PlatformSettings == null)
            {
                settings = default(T);
                return false;
            }

            foreach (IModioServiceSettings platform in PlatformSettings)
            {
                if (platform is not T t) 
                    continue;
                
                settings = t;
                return true;
            }

            settings = default(T);
            return false;
        }

        public ModioSettings ShallowClone() => MemberwiseClone() as ModioSettings;

        public void InvokeOnChanged() => OnSettingsUpdated?.Invoke(this);
    }
}
