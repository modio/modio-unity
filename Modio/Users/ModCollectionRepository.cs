using System;
using System.Collections.Generic;
using System.Linq;
using Modio.Collections;
using Modio.Mods;

namespace Modio.Users
{
    public class ModCollectionRepository : IDisposable
    {

        internal event Action OnContentsChanged;
        
        readonly HashSet<ModCollection> _followed = new HashSet<ModCollection>();
        
        public IEnumerable<ModCollection> GetFollowed() => _followed;

        internal ModCollectionRepository()
        {
            ModCollection.AddChangeListener(ModCollectionChangeType.IsFollowed, OnModCollectionFollowChange);
            ModioClient.OnShutdown += Dispose;
        }

        void OnModCollectionFollowChange(ModCollection mod, ModCollectionChangeType changeType)
        {
            var anyChange = false;
            if (mod.IsFollowed)
                anyChange |= _followed.Add(mod);
            else
                anyChange |= _followed.Remove(mod);

            if(anyChange) OnContentsChanged?.Invoke();
        }

        public bool IsFollowed(ModId modId) => _followed.Any(mod => mod.Id == modId);

        public void Dispose()
        {
            ModCollection.RemoveChangeListener(ModCollectionChangeType.IsFollowed, OnModCollectionFollowChange);
            ModioClient.OnShutdown -= Dispose;
        }
    }
}
