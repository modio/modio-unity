using System.Collections.Generic;

namespace ModIO.Implementation
{
    /// <summary>
    /// The serializable format for handling the system registry for all mod collections
    /// </summary>
    [System.Serializable]
    internal class ModCollectionRegistry
    {
        public Dictionary<long, UserModCollectionData> existingUsers =
            new Dictionary<long, UserModCollectionData>();

        public Dictionary<ModId, ModCollectionEntry> mods =
            new Dictionary<ModId, ModCollectionEntry>();
    }
}
