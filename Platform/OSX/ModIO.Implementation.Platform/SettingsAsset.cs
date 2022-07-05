using UnityEngine;

namespace ModIO.Implementation
{
    /// <summary>Osx extension to the SettingsAsset.</summary>
    internal partial class SettingsAsset : ScriptableObject
    {
        /// <summary>Configuration for Osx.</summary>
        public BuildSettings osxConfiguration;

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR

        /// <summary>Gets the configuration for OSX.</summary>
        public BuildSettings GetBuildSettings()
        {
            return this.osxConfiguration;
        }

#endif // UNITY_STANDALONE_WIN && !UNITY_EDITOR
    }
}
