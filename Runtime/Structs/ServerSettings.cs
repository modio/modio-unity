namespace ModIO
{
    /// <summary>
    /// Describes the server settings to use for the ModIO Plugin.
    /// This can be setup directly from the inspector when editing the config settings file, or you
    /// can instantiate and use this at runtime with the Initialize method
    /// </summary>
    /// <seealso cref="BuildSettings"/>
    /// <seealso cref="ModIOUnity.InitializeForUser"/>
    /// <seealso cref="ModIOUnityAsync.InitializeForUser"/>
    [System.Serializable]
    public struct ServerSettings
    {
        /// <summary>URL for the mod.io server to connect to.</summary>
        public string serverURL;

        /// <summary>Game Id as can be found on mod.io Web UI.</summary>
        public uint gameId;

        /// <summary>mod.io Service API Key used by your game to connect.</summary>
        public string gameKey;

        /// <summary>
        /// Language code for the localizing message responses.
        /// See https://docs.mod.io/#localization for possible values.
        /// </summary>
        public string languageCode;

        /// <summary>Disables uploading mods and modfiles for this build.</summary>
        public bool disableUploads;
    }
}
