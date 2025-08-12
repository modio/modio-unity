using System;
using System.Linq;
using Modio.API;
using Modio.API.Interfaces;
using Modio.Authentication;
using Modio.Extensions;
using Modio.Platforms;
using Modio.FileIO;
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
        static void OnAfterAssembliesLoaded()
        {
            var modioUnitySettings = Resources.Load<ModioUnitySettings>(ModioUnitySettings.DefaultResourceNameOverride);

            if (modioUnitySettings == null)
                modioUnitySettings = Resources.Load<ModioUnitySettings>(ModioUnitySettings.DefaultResourceName);

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
            
            ModioServices.Bind<IModioLogHandler>().FromNew<ModioUnityLogger>(ModioServicePriority.EngineImplementation);

            var environmentDetails = $"Unity; {Application.unityVersion}; {Application.platform}";
            ModioLog.Verbose?.Log(environmentDetails);

            Version.AddEnvironmentDetails(environmentDetails);

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
                         .WithInterfaces<IGetPortalProvider>()
                         .FromNew<WssAuthService>(
                             ModioServicePriority.PlatformProvided,
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

#if UNITY_EDITOR
        static void OnGameShuttingDown(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                ModioClient.Shutdown().ForgetTaskSafely();
        }
#endif

        static void Log(LogLevel logLevel, object message)
        {
            Action<object> log = logLevel switch
            {
                LogLevel.Error   => Debug.LogError,
                LogLevel.Warning => Debug.LogWarning,
                _                => Debug.Log,
            };

            log(message);
        }

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
