namespace ModIO.Implementation.API
{
    /// <summary>Holds the constants used by the server.</summary>
    internal static class ServerConstants
    {
        public static class HeaderKeys
        {
            public const string LANGUAGE = "accept-language";
            public const string PLATFORM = "x-modio-platform";
            public const string PLUGIN_VERSION = "user-agent";
            public const string PORTAL = "x-modio-portal";
        }

        /// <summary>Returns the portal header value for the given UserPortal.</summary>
        public static string ConvertUserPortalToHeaderValue(UserPortal portal)
        {
            string headerValue = null;

            switch(portal)
            {
                case UserPortal.Apple:
                {
                    headerValue = "apple";
                }
                break;

                case UserPortal.Discord:
                {
                    headerValue = "discord";
                }
                break;

                case UserPortal.EpicGamesStore:
                {
                    headerValue = "egs";
                }
                break;

                case UserPortal.GOG:
                {
                    headerValue = "gog";
                }
                break;

                case UserPortal.Google:
                {
                    headerValue = "google";
                }
                break;

                case UserPortal.itchio:
                {
                    headerValue = "itchio";
                }
                break;

                case UserPortal.Nintendo:
                {
                    headerValue = "nintendo";
                }
                break;

                case UserPortal.Oculus:
                {
                    headerValue = "oculus";
                }
                break;

                case UserPortal.PlayStationNetwork:
                {
                    headerValue = "psn";
                }
                break;

                case UserPortal.Steam:
                {
                    headerValue = "steam";
                }
                break;

                case UserPortal.XboxLive:
                {
                    headerValue = "xboxlive";
                }
                break;

                default:
                {
                    headerValue = "none";
                }
                break;
            }

            return headerValue;
        }
    }
}
