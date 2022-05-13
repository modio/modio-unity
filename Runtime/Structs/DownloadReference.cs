namespace ModIO
{
    /// <summary>
    /// Used in ModIOUnity.DownloadTexture() to get the Texture.
    /// (DownloadReference is serializable with Unity's JsonUtility)
    /// </summary>
    [System.Serializable]
    public struct DownloadReference
    {
        public ModId modId;
        public string url;
        public string filename;
    }
}
