using System;

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

    public static class SubscribedModStatusExtensions
    {
        public static bool IsSubscribed(this SubscribedModStatus value)
        {
            switch(value)
            {
                case SubscribedModStatus.Installed:
                case SubscribedModStatus.WaitingToDownload:
                case SubscribedModStatus.WaitingToInstall:
                case SubscribedModStatus.WaitingToUpdate:
                case SubscribedModStatus.Downloading:
                case SubscribedModStatus.Installing:
                case SubscribedModStatus.Updating:
                case SubscribedModStatus.None:
                    return true;

                case SubscribedModStatus.WaitingToUninstall:
                case SubscribedModStatus.Uninstalling:
                case SubscribedModStatus.ProblemOccurred:
                    return false;

                default:
                    break;
            }

            throw new NotImplementedException($"Unable to translate {value} of SubscribedMod");
        }
    }
}
