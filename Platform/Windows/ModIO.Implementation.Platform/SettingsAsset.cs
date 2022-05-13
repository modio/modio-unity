using UnityEngine;

namespace ModIO.Implementation
{
    /// <summary>Windows extension to the SettingsAsset.</summary>
    internal partial class SettingsAsset : ScriptableObject
    {
        /// <summary>Configuration for Windows.</summary>
        public BuildSettings windowsConfiguration;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        /// <summary>Gets the configuration for Windows.</summary>
        public BuildSettings GetBuildSettings()
        {
            return this.windowsConfiguration;
        }

#endif // UNITY_STANDALONE_WIN && !UNITY_EDITOR
    }
}
