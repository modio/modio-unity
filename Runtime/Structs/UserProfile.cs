namespace ModIO
{
    /// <summary>
    /// Represents a particular mod.io user with their username, DownloadReferences for getting
    /// their avatar, as well as their language and timezone.
    /// </summary>
    [System.Serializable]
    public struct UserProfile
    {
        /// <summary>
        /// The display name of the user's mod.io account
        /// </summary>
        public string username;

        /// <summary>
        ///  This is the unique Id of the user.
        /// </summary>
        public long userId;
        
        /// <summary>
        /// The display name of the user's account they authenticated with. Eg if they authenticated
        /// with Steam it would be their Steam username.
        /// </summary>
        public string portal_username;
        
        public DownloadReference avatar_original;
        public DownloadReference avatar_50x50;
        public DownloadReference avatar_100x100;
        public string timezone;
        public string language;
    }
}
