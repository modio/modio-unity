namespace ModIO
{
    public enum AuthenticationServiceProvider
    {
        Steam,
        Epic,
        GOG,
        Itchio,
        Oculus,
        Xbox,
        Switch,
        Discord,
        Google,
        PlayStation,
        OpenId,
        None,
    }

    public static class AuthenticationServiceProviderExtensions
    {
        public static string GetProviderName(this AuthenticationServiceProvider provider)
        {
            string providerName = "";

            switch(provider)
            {
                case AuthenticationServiceProvider.Steam:
                    providerName = "steamauth";
                    break;
                case AuthenticationServiceProvider.Epic:
                    providerName = "epicgamesauth";
                    break;
                case AuthenticationServiceProvider.GOG:
                    providerName = "galaxyauth";
                    break;
                case AuthenticationServiceProvider.Itchio:
                    providerName = "itchioauth";
                    break;
                case AuthenticationServiceProvider.Oculus:
                    providerName = "oculusauth";
                    break;
                case AuthenticationServiceProvider.Xbox:
                    providerName = "xboxauth";
                    break;
                case AuthenticationServiceProvider.Switch:
                    providerName = "switchauth";
                    break;
                case AuthenticationServiceProvider.Discord:
                    providerName = "discordauth";
                    break;
                case AuthenticationServiceProvider.Google:
                    providerName = "googleauth";
                    break;
                case AuthenticationServiceProvider.PlayStation:
                    providerName = "psnauth";
                    break;
                case AuthenticationServiceProvider.OpenId:
                    providerName = "openidauth";
                    break;
                case AuthenticationServiceProvider.None:
                    providerName = "none";
                    break;
            }

            return providerName;
        }

        public static string GetTokenFieldName(this AuthenticationServiceProvider provider)
        {
            string tokenFieldName = "";

            switch(provider)
            {
                case AuthenticationServiceProvider.Steam:
                    tokenFieldName = "appdata";
                    break;
                case AuthenticationServiceProvider.Epic:
                    tokenFieldName = "access_token";
                    break;
                case AuthenticationServiceProvider.GOG:
                    tokenFieldName = "appdata";
                    break;
                case AuthenticationServiceProvider.Itchio:
                    tokenFieldName = "itchio_token";
                    break;
                case AuthenticationServiceProvider.Oculus:
                    tokenFieldName = "access_token";
                    break;
                case AuthenticationServiceProvider.Xbox:
                    tokenFieldName = "xbox_token";
                    break;
                case AuthenticationServiceProvider.Switch:
                    tokenFieldName = "id_token";
                    break;
                case AuthenticationServiceProvider.Discord:
                    tokenFieldName = "discord_token";
                    break;
                case AuthenticationServiceProvider.Google:
                    tokenFieldName = "id_token";
                    break;
                case AuthenticationServiceProvider.PlayStation:
                    tokenFieldName = "auth_code";
                    break;
                case AuthenticationServiceProvider.OpenId:
                    tokenFieldName = "id_token";
                    break;
            }

            return tokenFieldName;
        }
    }
}
