using System.Collections.Generic;

namespace ModIO.Implementation
{
    [System.Serializable]
    internal class UserModCollectionData
    {
        public long userId;
        public HashSet<ModId> subscribedMods = new HashSet<ModId>();
        public List<ModId> unsubscribeQueue = new List<ModId>();
    }
}
