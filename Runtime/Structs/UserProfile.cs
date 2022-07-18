namespace ModIO
{
    /// <summary>
    /// Represents a particular mod.io user with their username, DownloadReferences for getting
    /// their avatar, as well as their language and timezone.
    /// </summary>
    [System.Serializable]
    public struct UserProfile
    {
        public string username;
        public DownloadReference avatar_original;
        public DownloadReference avatar_50x50;
        public DownloadReference avatar_100x100;
        public string timezone;
        public string language;
    }
}
