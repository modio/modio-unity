using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Modio.API;
using Modio.API.Interfaces;
using Modio.Authentication;
using Modio.Extensions;
using Modio.Platforms;
using Modio.FileIO;
using Modio.Monetization;
using Modio.Platforms.Wss;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Modio.Unity
{
    internal static class ModioUnity
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        [ExcludeFromCodeCoverage]
        static void OnAfterAssembliesLoaded()
        {
            ModioUnitySettings modioUnitySettings = LoadSettings();
            
            if (ModioCommandLine.TryGetArgument("gameid", out string gameId))
                modioUnitySettings.Settings.GameId = int.Parse(gameId);

            if (ModioCommandLine.TryGetArgument("apikey", out string apiKey))
                modioUnitySettings.Settings.APIKey = apiKey;

            if (ModioCommandLine.TryGetArgument("url", out string url))
                modioUnitySettings.Settings.ServerURL = url;
            
            if (ModioCommandLine.HasFlag("use-wss"))
                if(!modioUnitySettings.Settings.TryGetPlatformSettings(out WssSettings _))
                {
                    var wssSettings = new WssSettings();
                    modioUnitySettings.Settings.PlatformSettings = modioUnitySettings.Settings.PlatformSettings.Append(wssSettings).ToArray();
                }
            
            if (ModioCommandLine.TryGetArgument("monetizationtype", out string monetizationType))
            {
                if(modioUnitySettings.Settings.TryGetPlatformSettings(out MonetizationSettings monetizationSettings))
                    monetizationSettings.MonetizationType = Enum.Parse<ModioMonetizationType>(monetizationType, true);
                else
                {
                    monetizationSettings = new MonetizationSettings { MonetizationType = Enum.Parse<ModioMonetizationType>(monetizationType, true), };
                    modioUnitySettings.Settings.PlatformSettings = modioUnitySettings.Settings.PlatformSettings.Append(monetizationSettings).ToArray();
                    
                }
            }
            
            ModioServices.Bind<IModioLogHandler>().FromNew<ModioUnityLogger>(ModioServicePriority.EngineImplementation);

            var environmentDetails = $"Unity; {Application.unityVersion}; {Application.platform}";
            ModioLog.Verbose?.Log(environmentDetails);
            
            Error.StoreStackTraceWhenCreated = false;
             
            Version.AddEnvironmentDetails(environmentDetails);
            
            // If command line arg present we need to mutate the config
            if (ModioCommandLine.TryGetArgument("log", out string logLevelText)
                || ModioCommandLine.TryGetArgument("loglevel", out logLevelText))
            {
                if (Enum.TryParse(logLevelText, true, out LogLevel logLevelEnum))
                {
                    modioUnitySettings.Settings.LogLevel = logLevelEnum;
                }
                else
                    // ReSharper disable once ExpressionIsAlwaysNull (it's set in ApplyLogLevel)
                    // ReSharper disable once ConstantConditionalAccessQualifier
                    ModioLog.Error?.Log($"Unrecognized log level: {logLevelText}");
            }

            if (modioUnitySettings != null)
            {
                ModioServices.BindInstance(modioUnitySettings.Settings);
            }
            else
                ModioLog.Message?.Log(
                    $"Couldn't find a ModioUnitySettings named '{ModioUnitySettings.DefaultResourceName}' to load in a Resources folder"
                );

            // Uncomment the below for console implementations. Unity's web requests are not as reliable or informative
            // as standard HTTP requests, but standard HTTP requests will not qualify for XBOX (and potentially others)
            // certification requirements around Curl requests.
            ModioServices.Bind<IModioAPIInterface>().FromNew<ModioAPIUnityClient>(ModioServicePriority.EngineImplementation);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            ModioServices.Bind<IModioRootPathProvider>()
                         .FromNew<WindowsRootPathProvider>(
                             ModioServicePriority.PlatformProvided,
                             WindowsRootPathProvider.IsPublicEnvironmentVariableSet
                         );
#endif

            if (Application.platform == RuntimePlatform.LinuxPlayer)
                ModioServices.Bind<IModioDataStorage>()
                             .FromNew<LinuxDataStorage>(ModioServicePriority.PlatformProvided);

            if (Application.platform == RuntimePlatform.OSXPlayer)
                ModioServices.Bind<IModioDataStorage>()
                             .FromNew<MacDataStorage>(ModioServicePriority.PlatformProvided);

            ModioServices.Bind<IModioRootPathProvider>()
                         .FromNew<UnityRootPathProvider>(ModioServicePriority.Default);

            ModioServices.Bind<IWebBrowserHandler>()
                         .FromNew<UnityWebBrowserHandler>(ModioServicePriority.EngineImplementation);

            ModioServices.Bind<WssService>()
                         .FromNew<WssService>();

            ModioServices.Bind<WssAuthService>()
                         .WithInterfaces<IModioAuthService>()
                         .WithInterfaces<IGetActiveUserIdentifier>()
                         .FromNew<WssAuthService>(
                             ModioServicePriority.PlatformProvided-5, // Slightly lower priority than default platform auth services
                             () => ModioServices.Resolve<ModioSettings>()?.TryGetPlatformSettings(out WssSettings _)
                                   ?? false
                         );
            
            ModioServices.BindErrorMessage<ModioSettings>(
                "Please ensure you've bound a ModioSettings."
                + " You can create one using the menu item 'Tools/mod.io/Edit Settings'",
                ModioServicePriority.Fallback + 1
            );

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnGameShuttingDown;
#else
            Application.quitting += () => ModioClient.Shutdown().ForgetTaskSafely();
#endif

            InitPlatform();
        }

        [ExcludeFromCodeCoverage]
        static ModioUnitySettings LoadSettings()
        {
            ModioUnitySettings foundSetting = null;

            if (ModioCommandLine.TryGetArgument("unity-settings", out string target))
            {
                foundSetting = Resources.Load<ModioUnitySettings>($"mod.io/{target}");
                if (foundSetting == null)
                    foundSetting = Resources.Load<ModioUnitySettings>($"mod.io/v3_config_{target}");
            }

            if(foundSetting == null)
                foundSetting = Resources.Load<ModioUnitySettings>(ModioUnitySettings.DefaultResourceNameOverride);
            if (foundSetting == null)
                foundSetting = Resources.Load<ModioUnitySettings>(ModioUnitySettings.DefaultResourceName);

            return foundSetting;
        }


#if UNITY_EDITOR
        [ExcludeFromCodeCoverage]
        static void OnGameShuttingDown(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ModioClient.Shutdown().ForgetTaskSafely();
        }
#endif

        [ExcludeFromCodeCoverage]
        static void InitPlatform()
        {
            // Only contains RuntimePlatforms that have a corresponding ModioAPI.Platform.
            ModioAPI.Platform apiPlatform = Application.platform switch
            {
                RuntimePlatform.OSXEditor     => ModioAPI.Platform.Mac,
                RuntimePlatform.OSXPlayer     => ModioAPI.Platform.Mac,
                RuntimePlatform.WindowsPlayer => ModioAPI.Platform.Windows,
                RuntimePlatform.WindowsEditor => ModioAPI.Platform.Windows,
                RuntimePlatform.IPhonePlayer  => ModioAPI.Platform.IOS,
#if MODIO_OCULUS
                RuntimePlatform.Android            => ModioAPI.Platform.Oculus,
#else
                RuntimePlatform.Android => ModioAPI.Platform.Android,
#endif
                RuntimePlatform.LinuxPlayer        => ModioAPI.Platform.Linux,
                RuntimePlatform.LinuxEditor        => ModioAPI.Platform.Linux,
                RuntimePlatform.PS4                => ModioAPI.Platform.PlayStation4,
                RuntimePlatform.XboxOne            => ModioAPI.Platform.XboxOne,
                RuntimePlatform.Switch             => ModioAPI.Platform.Switch,
#if UNITY_6000_0_OR_NEWER
                RuntimePlatform.Switch2            => ModioAPI.Platform.Switch2,
#endif
                RuntimePlatform.GameCoreXboxSeries => ModioAPI.Platform.XboxSeriesX,
                RuntimePlatform.GameCoreXboxOne    => ModioAPI.Platform.XboxOne,
                RuntimePlatform.PS5                => ModioAPI.Platform.PlayStation5,
                _                                  => ModioAPI.Platform.None,
            };

            if (apiPlatform != ModioAPI.Platform.None)
                ModioAPI.SetPlatform(apiPlatform);
        }
    }
}
