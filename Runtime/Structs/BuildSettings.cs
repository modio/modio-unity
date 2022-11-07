using System.Collections.Generic;

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
        /// <summary>Level to log at.</summary>
        public LogLevel logLevel;

        /// <summary>Portal the game will be launched through.</summary>
        public UserPortal userPortal;

        // TODO Needs to be implemented alongside RequestCache.cs
        /// <summary>Size limit for the request cache.</summary>
        public uint requestCacheLimitKB;
    }
}
