
using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation
{
    [System.Serializable]
    internal class ModCollectionEntry
    {
        public ModfileObject currentModfile;
        public ModObject modObject;
        public bool uninstallIfNotSubscribedToCurrentSession;
    }
}
