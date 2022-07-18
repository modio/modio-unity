namespace ModIO
{
    /// <summary>
    /// Represents the ModProfile of a mod the current user has subscribed to. Contains the status
    /// and a directory (if installed) and the associated ModProfile.
    /// </summary>
    /// <remarks>Note this is not necessarily an installed mod. You will need to check
    /// the status to see whether or not it is installed.</remarks>
    /// <seealso cref="status"/>
    /// <seealso cref="SubscribedModStatus"/>
    /// <seealso cref="ModProfile"/>
    /// <seealso cref="ModIOUnity.GetSubscribedMods"/>
    public struct SubscribedMod
    {
        public SubscribedModStatus status;
        public string directory;
        public ModProfile modProfile;
    }
}
