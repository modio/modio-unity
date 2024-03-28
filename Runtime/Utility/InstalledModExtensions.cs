namespace ModIO.Implementation
{
    static class InstalledModExtensions
    {
        public static UserInstalledMod AsInstalledModsUser(this InstalledMod mod, long userId)
        {
            if(mod.subscribedUsers.Contains(userId))
            {
                return new UserInstalledMod()
                {
                    updatePending = mod.updatePending,
                    directory = mod.directory,
                    modProfile = mod.modProfile,
                    metadata = mod.metadata,
                    version = mod.version,
                    changeLog = mod.changeLog,
                    dateAdded = mod.dateAdded,
                    enabled = mod.enabled,
                };
            }

            return default;
        }
    }
}
