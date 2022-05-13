namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct UserEventObject
    {
        public long id;
        public long game_id;
        public long mod_id;
        public long user_id;
        public long date_added;
        public string event_type;
    }
}
