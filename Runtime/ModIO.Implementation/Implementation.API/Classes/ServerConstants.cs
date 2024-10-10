using System;

namespace ModIO.Implementation.API
{
    /// <summary>Holds the constants used by the server.</summary>
    internal static class ServerConstants
    {
        public static class HeaderKeys
        {
            public const string LANGUAGE = "accept-language";
            public const string PLATFORM = "x-modio-platform";
            public const string PORTAL = "x-modio-portal";
        }

        /// <summary>Returns the portal header value for the given UserPortal.</summary>
        public static string ConvertUserPortalToHeaderValue(UserPortal portal)
        {
            string headerValue = portal switch
            {
                UserPortal.Apple => "apple",
                UserPortal.Discord => "discord",
                UserPortal.EpicGamesStore => "egs",
                UserPortal.GOG => "gog",
                UserPortal.Google => "google",
                UserPortal.itchio => "itchio",
                UserPortal.Nintendo => "nintendo",
                UserPortal.Oculus => "oculus",
                UserPortal.PlayStationNetwork => "psn",
                UserPortal.Steam => "steam",
                UserPortal.XboxLive => "xboxlive",
                _ => null
            };

            return headerValue;
        }

        public static string ConvertPlatformToHeaderValue(RestApiPlatform platform)
        {
            return platform switch
            {
                RestApiPlatform.Windows => "windows",
                RestApiPlatform.Mac => "mac",
                RestApiPlatform.Linux => "linux",
                RestApiPlatform.XboxOne => "xboxone",
                RestApiPlatform.XboxSeriesX => "xboxseriesx",
                RestApiPlatform.Ps5 => "ps5",
                RestApiPlatform.Ps4 => "ps4",
                RestApiPlatform.Switch => "switch",
                //Backend does not currently support uwp
                RestApiPlatform.Uwp => "windows",
                RestApiPlatform.Android => "android",
                RestApiPlatform.Ios => "ios",
                RestApiPlatform.Oculus => "oculus",
                RestApiPlatform.Source => "source",
                _ => null
            };
        }
    }
}
