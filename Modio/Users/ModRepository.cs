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
        
        readonly HashSet<Mod> _created = new HashSet<Mod>();
        readonly HashSet<Mod> _subscribed = new HashSet<Mod>();
        readonly HashSet<Mod> _purchased = new HashSet<Mod>();
        readonly HashSet<Mod> _disabled = new HashSet<Mod>();
        
        /// <summary>
        /// <para>Returns a new array containing the <see cref="Mod"/>s this user added or is a team member of.</para>
        /// <para>Use <see cref="GetCreatedMods(List{Modio.Mods.Mod},out Modio.Error)"/> to avoid the array creation.</para>
        /// <para><b>Note:</b> mods may not be initialized, use Mod.<see cref="Mod.IsInitialized"/> to test for initialization.</para>
        /// </summary>
        public IEnumerable<Mod> GetCreatedMods() => _created;
        public IEnumerable<Mod> GetSubscribed() => _subscribed;
        public IEnumerable<Mod> GetPurchased() => _purchased;
        public IEnumerable<Mod> GetDisabled() => _disabled;

        internal ModRepository()
        {
            Mod.AddChangeListener(ModChangeType.IsSubscribed, OnModSubscriptionChange);
            Mod.AddChangeListener(ModChangeType.IsEnabled, OnModEnabledChange);
            Mod.AddChangeListener(ModChangeType.IsPurchased, OnModPurchasedChange);
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

        public bool IsSubscribed(ModId modId) => _subscribed.Any(mod => mod.Id == modId);
        public bool IsDisabled(ModId modId) => _disabled.Any(mod => mod.Id == modId);
        public bool IsPurchased(ModId modId) => _purchased.Any(mod => mod.Id == modId);

        public void Dispose()
        {
            Mod.RemoveChangeListener(ModChangeType.IsSubscribed, OnModSubscriptionChange);
            Mod.RemoveChangeListener(ModChangeType.IsEnabled, OnModEnabledChange);
            Mod.RemoveChangeListener(ModChangeType.IsPurchased, OnModPurchasedChange);
            ModioClient.OnShutdown -= Dispose;
        }
    }
}
