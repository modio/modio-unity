using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Modio.API.HttpClient;
using Modio.API.Interfaces;
using Modio.Authentication;
using Modio.Errors;

[assembly: InternalsVisibleTo("Modio.Unity")]
namespace Modio.API
{
    public static partial class ModioAPI
    {
        public enum Platform
        {
            None = -1,
            Source,
            Windows,
            Mac,
            Linux,
            Android,
            IOS,
            XboxOne,
            XboxSeriesX,
            PlayStation4,
            PlayStation5,
            Switch,
            Oculus,
        }

        public enum Portal
        {
            None = -1,
            Apple,
            Discord,
            EpicGamesStore,
            Facebook,
            GOG,
            Google,
            Itchio,
            Nintendo,
            PlayStationNetwork,
            SSO,
            Steam,
            XboxLive,
        }

        public static event Action<bool> OnOfflineStatusChanged;

        public static bool IsOffline { get; private set; }

        public static Portal CurrentPortal { get; private set; } = Portal.None;

        const string HEADER_LANGUAGE_RESPONSE = "Accept-Language";
        const string HEADER_PLATFORM = "X-Modio-Platform";
        const string HEADER_PORTAL = "X-Modio-Portal";

        static string _serverURL;
        static ModioSettings _modioSettings;

        public static string LanguageCodeResponse { get; private set; } = "en";
        static Platform _platform = Platform.None;

        static IModioAPIInterface _apiInterface;

        /// <summary>
        /// Initialises the ModioAPI
        /// </summary>
        public static void Init()
        {
            _modioSettings = ModioServices.Resolve<ModioSettings>();

            _serverURL = string.IsNullOrWhiteSpace(_modioSettings.ServerURL) ? $"https://g-{_modioSettings.GameId}.modapi.io/v1" : _modioSettings.ServerURL;

            ModioLog.Verbose?.Log($"Initialized {Version.GetCurrent()}");
            ModioLog.Verbose?.Log(_modioSettings.ServerURL == null ? _serverURL : $"{_modioSettings.GameId}; {_modioSettings.ServerURL}");
            
            var apiInterfaceBinding = ModioServices.GetBindings<IModioAPIInterface>();
            SetAPIInterface(apiInterfaceBinding.Resolve());
            // We need to handle the api interface changing mostly for unit tests
            apiInterfaceBinding.OnNewBinding -= SetAPIInterface;
            apiInterfaceBinding.OnNewBinding += SetAPIInterface;
            
            var authServiceBinding = ModioServices.GetBindings<IModioAuthService>();
            
            SetPortalFromPortalProvider(authServiceBinding.Resolve());
            // We need to handle the api interface changing mostly for unit tests
            authServiceBinding.OnNewBinding -= SetPortalFromPortalProvider;
            authServiceBinding.OnNewBinding += SetPortalFromPortalProvider;
        }

#region Language

        /// <summary>
        /// Sets the response language from the given language code.
        /// </summary>
        /// <param name="languageCode">The language code</param>
        public static void SetResponseLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                ModioLog.Message?.Log($@"{nameof(ModioAPI)} response language is invalid (""{languageCode}""). Use {nameof(ModioAPI)}.{nameof(SetResponseLanguage)} to set a valid language code. Defaulting to [en]");
                languageCode = "en";
            }

            LanguageCodeResponse = languageCode;

            if (_apiInterface == null) return;

            _apiInterface.RemoveDefaultHeader(HEADER_LANGUAGE_RESPONSE);
            _apiInterface.SetDefaultHeader(HEADER_LANGUAGE_RESPONSE, languageCode);
        }

#endregion

        /// <summary>
        /// Sets the platform header from the given <see cref="Platform"/>
        /// </summary>
        /// <param name="platform">The platform</param>
        public static void SetPlatform(Platform platform)
        {
            if (platform == Platform.None)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    platform = Platform.Windows;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    platform = Platform.Mac;
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    platform = Platform.Linux;
            }

            _platform = platform;

            if (_apiInterface == null) return;

            _apiInterface.RemoveDefaultHeader(HEADER_PLATFORM);
            if (platform.GetHeader() != null) _apiInterface.SetDefaultHeader(HEADER_PLATFORM, platform.GetHeader());
        }
        
        /// <summary>
        /// Sets the portal header from the given portal.
        /// </summary>
        /// <param name="portal"></param>
        public static void SetPortal(Portal portal)
        {
            CurrentPortal = portal;

            if (_apiInterface == null) return;

            _apiInterface.RemoveDefaultHeader(HEADER_PORTAL);
            string header = portal.GetHeader();
            if (header != null) _apiInterface.SetDefaultHeader(HEADER_PORTAL, header);
        }

        static void SetPortalFromPortalProvider(IModioAuthService authService)
        {
            SetPortal(authService?.Portal ?? Portal.None);
        }

        /// <summary>
        /// sets the current interface to be used by the ModioAPI
        /// </summary>
        /// <param name="apiInterface">The new apiInterface</param>
        public static void SetAPIInterface(IModioAPIInterface apiInterface)
        {
            apiInterface.ResetConfiguration();

            _apiInterface = apiInterface;
            apiInterface.SetDefaultHeader("Accept", "application/json");
            SetResponseLanguage(LanguageCodeResponse);
            apiInterface.SetDefaultHeader("User-Agent", Version.GetCurrent());
            SetPlatform(_platform);
            SetPortal(CurrentPortal);

            _apiInterface.AddDefaultParameter($"api_key={_modioSettings.APIKey}");
            _apiInterface.SetBasePath(_serverURL);
            _apiInterface.AddDefaultPathParameter("game-id", $"{_modioSettings.GameId}");

            ModioLog.Verbose?.Log($"{nameof(ModioAPI)}.{nameof(SetAPIInterface)}({_apiInterface.GetType().Name})");
        }

        /// <summary>
        /// Sets the ModioAPI to be offline
        /// </summary>
        /// <param name="isOffline">is it offline?</param>
        public static void SetOfflineStatus(bool isOffline)
        {
            if (IsOffline == isOffline) return;
            
            IsOffline = isOffline;
            OnOfflineStatusChanged?.Invoke(isOffline);
        }

        static bool IsInitialized()
        {
            if (_modioSettings.GameId != 0) return true;

            ModioLog.Error?.Log(ErrorCode.API_NOT_INITIALIZED.GetMessage());
            return false;
        }

        /// <summary>
        /// Pings the mod.io api
        /// </summary>
        /// <returns>
        /// An asynchronous task that returns <c>true</c> on successful ping.
        /// </returns>
        public static async Task<bool> Ping()
        {
            (var error, _) = await General.Ping();
            
            return !error;
        } 

#region GetHeader Extensions

        public static Platform PlatformFromHeader(string platform) => platform switch
        {
            "source"      => Platform.Source,
            "windows"     => Platform.Windows,
            "mac"         => Platform.Mac,
            "linux"       => Platform.Linux,
            "android"     => Platform.Android,
            "ios"         => Platform.IOS,
            "xboxone"     => Platform.XboxOne,
            "xboxseriesx" => Platform.XboxSeriesX,
            "ps4"         => Platform.PlayStation4,
            "ps5"         => Platform.PlayStation5,
            "switch"      => Platform.Switch,
            "oculus"      => Platform.Oculus,
            _             => Platform.None,
        };
        
        static string GetHeader(this Platform platform) => platform switch
            {
                Platform.Source       => "source",
                Platform.Windows      => "windows",
                Platform.Mac          => "mac",
                Platform.Linux        => "linux",
                Platform.Android      => "android",
                Platform.IOS          => "ios",
                Platform.XboxOne      => "xboxone",
                Platform.XboxSeriesX  => "xboxseriesx",
                Platform.PlayStation4 => "ps4",
                Platform.PlayStation5 => "ps5",
                Platform.Switch       => "switch",
                Platform.Oculus       => "oculus",
                _                     => null,
            };

        static string GetHeader(this Portal portal) => portal switch
            {
                Portal.Apple              => "apple",
                Portal.Discord            => "discord",
                Portal.EpicGamesStore     => "epicgames",
                Portal.Facebook           => "facebook",
                Portal.GOG                => "gog",
                Portal.Google             => "google",
                Portal.Itchio             => "itchio",
                Portal.Nintendo           => "nintendo",
                Portal.PlayStationNetwork => "psn",
                Portal.SSO                => "sso",
                Portal.Steam              => "steam",
                Portal.XboxLive           => "xboxlive",
                _                         => null,
            };

#endregion GetHeader Extensions
    }
}
