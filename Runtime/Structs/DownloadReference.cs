namespace ModIO
{
    /// <summary>
    /// Used in ModIOUnity.DownloadTexture() to get the Texture.
    /// (DownloadReference is serializable with Unity's JsonUtility)
    /// </summary>
    /// <seealso cref="ModIOUnity.DownloadTexture"/>
    /// <seealso cref="ModIOUnityAsync.DownloadTexture"/>
    [System.Serializable]
    public struct DownloadReference
    {
        public ModId modId;
        public string url;
        public string filename;

        /// <summary>
        /// Check if there is a valid url for this image. You may want to check this before using
        /// the ModIOUnity.DownloadTexture method.
        /// </summary>
        /// <seealso cref="ModIOUnity.DownloadTexture"/>
        /// <seealso cref="ModIOUnityAsync.DownloadTexture"/>
        /// <returns>true if the url isn't null</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(url);
        }
    }
}
