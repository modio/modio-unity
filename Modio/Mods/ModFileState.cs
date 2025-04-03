namespace Modio.Mods
{
    public enum ModFileState
    {
        /// <summary>Mod has not been downloaded.</summary>
        None,
        /// <summary>Mod has been queued for download & install.</summary>
        Queued,
        /// <summary>Mod is being downloaded for the first time.</summary>
        Downloading,
        /// <summary>Mod has been downloaded and is awaiting install.</summary>
        Downloaded,
        /// <summary>Mod is downloaded and being installed.</summary>
        Installing,
        /// <summary>Mod is installed.</summary>
        Installed,
        /// <summary>Mod is installed and an update is being downloaded.</summary>
        Updating,
        /// <summary>Mod is being uninstalled.</summary>
        Uninstalling,
        /// <summary>An operation failure has occured when installing/uninstalling this mod.</summary>
        FileOperationFailed,
    }
}
