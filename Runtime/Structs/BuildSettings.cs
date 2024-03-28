using UnityEngine;

namespace ModIO
{
    /// <summary>
    /// Build-specific configuration values. This can be setup directly from the inspector when
    /// editing the config settings file, or you can instantiate and use this at runtime with the
    /// Initialize method
    /// </summary>
    /// <seealso cref="ServerSettings"/>
    /// <seealso cref="ModIOUnity.InitializeForUser"/>
    /// <seealso cref="ModIOUnityAsync.InitializeForUser"/>
    [System.Serializable]
    public class BuildSettings
    {
        public BuildSettings() { }

        public BuildSettings(BuildSettings buildSettings)
        {
            this.logLevel = buildSettings.logLevel;
            this.userPortal = buildSettings.userPortal;
            this.requestCacheLimitKB = buildSettings.requestCacheLimitKB;
            this.defaultPortal = buildSettings.defaultPortal;
        }

        /// <summary>Level to log at.</summary>
        [HideInInspector] public LogLevel logLevel;

        /// <summary>Portal the game will be launched through.</summary>
        public UserPortal userPortal = UserPortal.None;

        /// <summary>Default portal.</summary>
        [HideInInspector] public UserPortal defaultPortal = UserPortal.None;

        // TODO Needs to be implemented alongside RequestCache.cs
        /// <summary>Size limit for the request cache.</summary>
        public uint requestCacheLimitKB;

        public void SetDefaultPortal()
        {
            this.userPortal = this.defaultPortal;
        }
    }
}
