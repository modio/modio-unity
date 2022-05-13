using UnityEngine;

namespace ModIO.Implementation
{
    /// <summary>Asset representation of a collection of build-settings.</summary>
    internal partial class SettingsAsset : ScriptableObject
    {
#region Asset Management

        /// <summary>Data path for the asset.</summary>
        public const string FilePath = @"mod.io/config";

        /// <summary>Loads the settings asset at the default path.</summary>
        public static Result TryLoad(out ServerSettings serverSettings,
                                     out BuildSettings buildSettings)
        {
            SettingsAsset asset = Resources.Load<SettingsAsset>(SettingsAsset.FilePath);

            if(asset == null)
            {
                serverSettings = new ServerSettings();
                buildSettings = new BuildSettings();

                return ResultBuilder.Create(ResultCode.Init_FailedToLoadConfig);
            }
            else
            {
                serverSettings = asset.serverSettings;
                buildSettings = asset.GetBuildSettings();

                Resources.UnloadAsset(asset);

                return ResultBuilder.Success;
            }
        }

#endregion // Asset Management

#region Data

        /// <summary>Server Settings</summary>
        public ServerSettings serverSettings;

        // NOTE(@jackson):
        //  The following section is the template for what a platform-specific implementation
        //  should look like. The platform partial will include a BuildSettings field
        //  that is exposed without protection and an implementation of GetBuildSettings()
        //  protected by a platform pre-processor.

        /// <summary>Configuration for the editor.</summary>
        public BuildSettings editorConfiguration;

#if UNITY_EDITOR

        /// <summary>Gets the configuration for the editor.</summary>
        public BuildSettings GetBuildSettings()
        {
            return this.editorConfiguration;
        }

#endif // UNITY_EDITOR
#endregion // Data
    }
}
