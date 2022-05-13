namespace ModIO
{
    /// <summary>
    /// The current state of a subscribed mod. Useful for checking whether or not a mod has been
    /// installed yet or if there was a problem trying to download/install it.
    /// </summary>
    /// <seealso cref="SubscribedMod"/>
    public enum SubscribedModStatus
    {
        Installed,
        WaitingToDownload,
        WaitingToInstall,
        WaitingToUpdate,
        WaitingToUninstall,
        Downloading,
        Installing,
        Uninstalling,
        Updating,
        ProblemOccurred,
        None,
    }
}
