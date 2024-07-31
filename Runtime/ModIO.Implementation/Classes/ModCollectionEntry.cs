using ModIO.Implementation.API.Objects;
using Runtime.Enums;

namespace ModIO.Implementation
{
    [System.Serializable]
    internal class ModCollectionEntry
    {
        public ModfileObject currentModfile;
        public ModObject modObject;
        public bool uninstallIfNotSubscribedToCurrentSession;
        public ModPriority priority = ModPriority.Normal;
    }
}
