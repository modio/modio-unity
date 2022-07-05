using UnityEngine;

namespace ModIO.Implementation
{
    /// <summary>Linux extension to the SettingsAsset.</summary>
    internal partial class SettingsAsset : ScriptableObject
    {
        /// <summary>Configuration for Linux.</summary>
        public BuildSettings linuxConfiguration;

#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR

        /// <summary>Gets the configuration for Linux.</summary>
        public BuildSettings GetBuildSettings()
        {
            return this.linuxConfiguration;
        }

#endif // UNITY_STANDALONE_WIN && !UNITY_EDITOR
    }
}
