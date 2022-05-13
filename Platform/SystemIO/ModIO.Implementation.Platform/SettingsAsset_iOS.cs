using UnityEngine;

namespace ModIO.Implementation
{
    /// <summary>iOS extension to the SettingsAsset.</summary>
    internal partial class SettingsAsset : ScriptableObject
    {
        /// <summary>Configuration for iOS.</summary>
        public BuildSettings iosConfiguration;

#if UNITY_IOS && !UNITY_EDITOR

        /// <summary>Gets the configuration for iOS.</summary>
        public BuildSettings GetBuildSettings()
        {
            return this.iosConfiguration;
        }

#endif // UNITY_STANDALONE_WIN && !UNITY_EDITOR
    }
}
