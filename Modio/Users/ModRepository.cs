using System;
using System.Collections.Generic;
using System.Linq;
using Modio.Mods;

namespace Modio.Users
{
    public class ModRepository : IDisposable
    {
        public bool HasGotSubscriptions { get; internal set; } = false;

        internal event Action OnContentsChanged;
        
        readonly HashSet<Mod> _subscribed = new HashSet<Mod>();
        readonly HashSet<Mod> _purchased = new HashSet<Mod>();
        readonly HashSet<Mod> _disabled = new HashSet<Mod>();
        readonly HashSet<Mod> _creations = new HashSet<Mod>();
        
        public IEnumerable<Mod> GetCreatedMods() => _creations;
        public IEnumerable<Mod> GetSubscribed() => _subscribed;
        public IEnumerable<Mod> GetPurchased() => _purchased;
        public IEnumerable<Mod> GetDisabled() => _disabled;

        internal ModRepository()
        {
            Mod.AddChangeListener(ModChangeType.IsSubscribed, OnModSubscriptionChange);
            Mod.AddChangeListener(ModChangeType.IsEnabled, OnModEnabledChange);
            Mod.AddChangeListener(ModChangeType.IsPurchased, OnModPurchasedChange);
            Mod.AddChangeListener(ModChangeType.IsUserCreation, OnModUserCreationStatusChange);
            ModioClient.OnShutdown += Dispose;
        }

        void OnModSubscriptionChange(Mod mod, ModChangeType changeType)
        {
            var anyChange = false;
            if (mod.IsSubscribed)
                anyChange |= _subscribed.Add(mod);
            else
            {
                anyChange |= _subscribed.Remove(mod);
                anyChange |= _disabled.Remove(mod);
            }
            if(anyChange) OnContentsChanged?.Invoke();
        }

        void OnModEnabledChange(Mod mod, ModChangeType changeType)
        {
            var anyChange = false;
            if (!mod.IsEnabled)
                anyChange |= _disabled.Add(mod);
            else
                anyChange |= _disabled.Remove(mod);
            if(anyChange) OnContentsChanged?.Invoke();
        }

        void OnModPurchasedChange(Mod mod, ModChangeType changeType)
        {
            var anyChange = false;
            if (mod.IsPurchased)
                anyChange |= _purchased.Add(mod);
            else
                anyChange |= _purchased.Remove(mod);
            if(anyChange) OnContentsChanged?.Invoke();
        }

        void OnModUserCreationStatusChange(Mod mod, ModChangeType changeType)
        {
            var anyChange = false;
            if (mod.IsCreated)
                anyChange |= _creations.Add(mod);
            else
                anyChange |= _creations.Remove(mod);
            if(anyChange) OnContentsChanged?.Invoke();
        }

        public void RemoveMod(Mod mod)
        {
            var anyChange = false;
            anyChange |= _subscribed.Remove(mod);
            anyChange |= _disabled.Remove(mod);
            anyChange |= _purchased.Remove(mod);
            if(anyChange) OnContentsChanged?.Invoke();
        }

        public bool IsSubscribed(ModioId modId) => _subscribed.Any(mod => mod.Id == modId);
        public bool IsDisabled(ModioId modId) => _disabled.Any(mod => mod.Id == modId);
        public bool IsPurchased(ModioId modId) => _purchased.Any(mod => mod.Id == modId);

        public void Dispose()
        {
            Mod.RemoveChangeListener(ModChangeType.IsSubscribed, OnModSubscriptionChange);
            Mod.RemoveChangeListener(ModChangeType.IsEnabled, OnModEnabledChange);
            Mod.RemoveChangeListener(ModChangeType.IsPurchased, OnModPurchasedChange);
            Mod.RemoveChangeListener(ModChangeType.IsUserCreation, OnModUserCreationStatusChange);
            ModioClient.OnShutdown -= Dispose;
        }
    }
}
