using UnityEngine;

namespace ModIO.Implementation
{
    /// <summary>Windows extension to the SettingsAsset.</summary>
    internal partial class SettingsAsset : ScriptableObject
    {
        /// <summary>Configuration for Android.</summary>
        public BuildSettings androidConfiguration;

#if UNITY_ANDROID && !UNITY_EDITOR

        /// <summary>Gets the configuration for Android.</summary>
        public BuildSettings GetBuildSettings()
        {
            return this.androidConfiguration;
        }

#endif // UNITY_STANDALONE_WIN && !UNITY_EDITOR
    }
}
