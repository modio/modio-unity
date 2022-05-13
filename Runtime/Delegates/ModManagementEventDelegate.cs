namespace ModIO
{
    /// <summary>
    /// A delegate that gets invoked each time a new ModManagement event happens (download, install,
    /// subscribe, etc)
    /// </summary>
    public delegate void ModManagementEventDelegate(ModManagementEventType eventType, ModId modId);
}
