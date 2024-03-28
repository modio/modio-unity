namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct GameObject
    {
        // NOTE STEVE: There are many more fields that can be serialized but for now I'm just adding the somewhat relevant ones
        public string name;
        public string ugc_name;
        public GameStatsObject stats;
        public string token_name;
        public GameMonetizationOptions monetization_options;
    }
}
