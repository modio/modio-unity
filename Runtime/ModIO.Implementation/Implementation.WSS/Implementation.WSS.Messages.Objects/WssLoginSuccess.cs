namespace ModIO.Implementation.Wss.Messages.Objects
{
    [System.Serializable]
    internal struct WssLoginSuccess
    {
        public long code;
        public string access_token;
        public long date_expires;
    }
}
