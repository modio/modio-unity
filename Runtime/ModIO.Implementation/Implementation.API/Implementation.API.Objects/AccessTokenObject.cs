namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct AccessTokenObject
    {
        public long code;
        public string access_token;
        public long date_expires;
    }
}
