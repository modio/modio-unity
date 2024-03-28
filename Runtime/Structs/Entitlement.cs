using System;

namespace ModIO
{
    /// <summary>
    /// A struct representing all of the information available for an Entitlement.
    /// </summary>
    /// <seealso cref="ModIOUnity.SyncEntitlements"/>
    /// <seealso cref="ModIOUnityAsync.SyncEntitlements"/>
    [Serializable]
    public struct Entitlement
    {
        public string transactionId;
        public int transactionState;
        public string skuId;
        public bool entitlementConsumed;
    }
}
