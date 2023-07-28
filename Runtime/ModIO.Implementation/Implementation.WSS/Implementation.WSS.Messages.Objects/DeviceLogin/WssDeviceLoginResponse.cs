namespace ModIO.Implementation.Wss.Messages.Objects
{
    [System.Serializable]
    internal struct WssDeviceLoginResponse
    {
        public string code;
        public long date_expires;
        public string display_url;
        public string login_url;
    }
}
