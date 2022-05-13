namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct UserObject
    {
        public long id;
        public string name_id;
        public string username;
        public string display_name_portal;
        public long date_online;
        public AvatarObject avatar;
        public string timezone;
        public string language;
        public string profile_url;
    }
}
