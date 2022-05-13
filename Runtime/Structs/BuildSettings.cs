using System.Collections.Generic;

namespace ModIO
{
    /// <summary>Build-specific configuration values.</summary>
    [System.Serializable]
    public struct BuildSettings
    {
        /// <summary>Level to log at.</summary>
        public LogLevel logLevel;

        /// <summary>Portal the game will be launched through.</summary>
        public UserPortal userPortal;

        /// <summary>Size limit for the request cache.</summary>
        public uint requestCacheLimitKB;

        /// <summary>Extra values necessary for the initialization of a platform.</summary>
        public List<string> extData;
    }
}
