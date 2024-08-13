using System;
using ModIO.Implementation;

namespace ModIO
{
    public static class ModIOUnityEvents
    {
        /// <summary>
        /// Call the passed method when the plugin is initialized. Calls immediately if already initialized
        /// </summary>
        public static event Action PluginInitialized
        {
            add
            {
                PluginInitializedInternal += value;
                if(ModIOUnityImplementation.isInitialized)
                    value.Invoke();
            }
            remove => PluginInitializedInternal -= value;
        }
        static event Action PluginInitializedInternal;

        internal static void OnPluginInitialized() => PluginInitializedInternal?.Invoke();

        public static event Action<UserProfile?> UserAuthenticationChanged;
        public static event Action UserEntitlementsChanged;
        internal static void OnUserAuthenticated(UserProfile? user = null) => UserAuthenticationChanged?.Invoke(user);
        internal static void OnUserRemoved() => UserAuthenticationChanged?.Invoke(null);
        internal static void OnUserEntitlementsChanged() => UserEntitlementsChanged?.Invoke();

        public static bool TryGetCachedMod(ModId modId, out ModProfile modProfile) => ModCollectionManager.TryGetModProfile(modId, out modProfile);

        public static event Action<ModId, bool> ModSubscriptionIntentChanged;
        public static event Action<ModId> ModSubscriptionInfoChanged;
        public static event Action<ModId, bool> ModPurchasedChanged;
        internal static void InvokeModSubscriptionChanged(ModId modId, bool isSubscribed) => ModSubscriptionIntentChanged?.Invoke(modId, isSubscribed);
        internal static void InvokeModSubscriptionInfoChanged(ModId modId) => ModSubscriptionInfoChanged?.Invoke(modId);
        internal static void InvokeModPurchasedChanged(ModId modId, bool isPurchased) => ModPurchasedChanged?.Invoke(modId, isPurchased);

        public static void RemoveModManagementEventDelegate(ModManagementEventDelegate modManagementEventDelegate) => ModManagement.modManagementEventDelegate -= modManagementEventDelegate;
    }
}
