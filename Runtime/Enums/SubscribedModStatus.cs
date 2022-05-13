namespace ModIO
{
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
