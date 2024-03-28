namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct MonetizationTeamAccountsObject
    {
        public long id; //user ID
        public string name_id;
        public string username;
        public int monetization_status;
        public int monetization_options;
        public int split;
    }
}
