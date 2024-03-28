#if UNITY_2019_4_OR_NEWER
using System;
using UnityEngine;

namespace ModIO.Implementation
{
#region Placeholder Build Settings
    //Placeholders which are filled out when adding the corresponding package.
    [Serializable] public partial class SwitchBuildSettings : BuildSettings {}
    [Serializable] public partial class GameCoreBuildSettings : BuildSettings {}
    [Serializable] public partial class PlaystationBuildSettings : BuildSettings {}
#endregion

    /// <summary>Asset representation of a collection of build-settings.</summary>
    internal class SettingsAsset : ScriptableObject
    {
        private void Awake()
        {
            editorConfiguration.logLevel = editorLogLevel;
            iosConfiguration.userPortal = UserPortal.Apple;
            iosConfiguration.logLevel = playerLogLevel;
            standaloneConfiguration.userPortal = UserPortal.None;
            standaloneConfiguration.logLevel = playerLogLevel;
            androidConfiguration.userPortal = UserPortal.Google;
            androidConfiguration.logLevel = playerLogLevel;
            switchConfiguration.userPortal = UserPortal.Nintendo;
            switchConfiguration.logLevel = playerLogLevel;
            playstationConfiguration.userPortal = UserPortal.PlayStationNetwork;
            playstationConfiguration.logLevel = playerLogLevel;
            gameCoreConfiguration.userPortal = UserPortal.XboxLive;
            gameCoreConfiguration.logLevel = playerLogLevel;
        }

#region Asset Management

        /// <summary>Data path for the asset.</summary>
        public const string FilePath = @"mod.io/config";

        /// <summary>Loads the settings asset at the default path.</summary>
        public static Result TryLoad(out ServerSettings serverSettings,
                                     out BuildSettings buildSettings)
        {
            SettingsAsset asset = Resources.Load<SettingsAsset>(FilePath);

            if(asset == null)
            {
                serverSettings = new ServerSettings();
                buildSettings = new BuildSettings();
                return ResultBuilder.Create(ResultCode.Init_FailedToLoadConfig);
            }

            serverSettings = asset.serverSettings;
            buildSettings = asset.GetBuildSettings();
            Resources.UnloadAsset(asset);

            return ResultBuilder.Success;
        }

        public static Result TryLoad(out bool autoInitializePlugin)
        {
            SettingsAsset asset = Resources.Load<SettingsAsset>(FilePath);

            if(asset == null)
            {
                autoInitializePlugin = false;
                return ResultBuilder.Create(ResultCode.Init_FailedToLoadConfig);
            }

            autoInitializePlugin = asset.autoInitializePlugin;
            Resources.UnloadAsset(asset);
            return ResultBuilder.Success;
        }

        #endregion // Asset Management

#region Data

        /// <summary>Server Settings</summary>
        [HideInInspector]
        public ServerSettings serverSettings;

        // NOTE(@jackson):
        //  The following section is the template for what a platform-specific implementation
        //  should look like. The platform partial will include a BuildSettings field
        //  that is exposed without protection and an implementation of GetBuildSettings()
        //  protected by a platform pre-processor.

        //Initializes the ModIO plugin, with default settings, the first time it is used
        [SerializeField] private bool autoInitializePlugin = true;

        /// <summary>Level to log at.</summary>
        [SerializeField] private LogLevel playerLogLevel;
        /// <summary>Level to log at.</summary>
        [SerializeField] private LogLevel editorLogLevel;
        /// <summary>Configuration for iOS.</summary>
        [SerializeField] private BuildSettings iosConfiguration;
        /// <summary>Configuration for Windows.</summary>
        [SerializeField] private BuildSettings standaloneConfiguration;
        /// <summary>Configuration for Android.</summary>
        [SerializeField] private BuildSettings androidConfiguration;
        /// <summary>Configuration for Switch.</summary>
        [SerializeField] private SwitchBuildSettings switchConfiguration;
        /// <summary>Configuration for Gamecore.</summary>
        [SerializeField] private GameCoreBuildSettings gameCoreConfiguration;
        /// <summary>Configuration for Playstation.</summary>
        [SerializeField] private PlaystationBuildSettings playstationConfiguration;
        /// <summary>Configuration for the editor.</summary>
        [SerializeField] private BuildSettings editorConfiguration;

        private BuildSettings GetBuildSettings()
        {
    #if (UNITY_PS4 || UNITY_PS5)
            return playstationConfiguration;
    #elif UNITY_SWITCH
            return switchConfiguration;
    #elif UNITY_GAMECORE
            return gameCoreConfiguration;
    #elif UNITY_IOS
            return this.iosConfiguration;
    #elif (UNITY_STANDALONE || UNITY_WSA)
            return this.standaloneConfiguration;
    #elif UNITY_ANDROID
            return this.androidConfiguration;
    #elif UNITY_EDITOR
            return this.editorConfiguration;
    #endif
        }

#endregion // Data
    }
}
#endif//UNITY_2019_4_OR_NEWER
