using System.Collections.Generic;

namespace ModIO
{
    /// <summary>
    /// Struct used to represent a mod that already exists on the current device. You can view the
    /// subscribed users to this mod as well as the directory and modprofile associated to it.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetSystemInstalledMods"/>
    /// <seealso cref="ModProfile"/>
    public struct InstalledMod
    {
        public List<long> subscribedUsers;
        public bool updatePending;
        public string directory;
        public ModProfile modProfile;
    }
}
