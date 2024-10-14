#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
    #define DISABLESTEAMWORKS
#elif UNITY_FACEPUNCH
    #define DISABLESTEAMWORKS
#endif

#if UNITY_STEAMWORKS && !DISABLESTEAMWORKS
using Steamworks;
#endif

using System.Collections.Generic;
using UnityEngine;

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformExampleSteamworks : MonoBehaviour
    {
        [SerializeField] private uint appId;

        void Awake()
        {
#if UNITY_STEAMWORKS && !DISABLESTEAMWORKS
            bool success = SteamAPI.Init();

            if (!success)
            {
                Debug.Log($"Steamworks.net was unable to initialize; destroying {nameof(ModioPlatformExampleSteamworks)}");
                Destroy(this);
                return;
            }

            steamInventoryStartPurchaseResult = CallResult<SteamInventoryStartPurchaseResult_t>.Create(this.OnSteamInventoryStartPurchaseResult);
            steamInventoryResultReady = CallResult<SteamInventoryResultReady_t>.Create(this.OnSteamInventoryResultReady);

            // --- This is the important line to include in your own implementation ---
            ModioPlatformSteamworks.SetAsPlatform(appId);
#else
            Destroy(this);
            return;
#endif
        }
#if UNITY_STEAMWORKS && !DISABLESTEAMWORKS
        public struct SteamCartItem
        {
            public SteamItemDef_t ItemDef;
            public uint Quantity;
        }

        private CallResult<SteamInventoryStartPurchaseResult_t> steamInventoryStartPurchaseResult;
        private CallResult<SteamInventoryResultReady_t> steamInventoryResultReady;
        private readonly List<SteamCartItem> cart = new List<SteamCartItem>();

        protected virtual void OnDestroy()
        {
            SteamAPI.Shutdown();
        }

        protected virtual void Update()
        {
            // Run Steam client callbacks
            SteamAPI.RunCallbacks();
        }


        private void OnSteamInventoryStartPurchaseResult(SteamInventoryStartPurchaseResult_t response, bool ioFailure)
        {
            if (response.m_result == EResult.k_EResultOK)
                Debug.Log("Purchase Started!");
        }

        private void OnSteamInventoryResultReady(SteamInventoryResultReady_t response, bool ioFailure)
        {
            if (response.m_result == EResult.k_EResultOK)
                Debug.Log("Purchase Completed!");
        }

        public void StartPurchase()
        {
            this.AddToCart(new SteamItemDef_t(1000), 1);
            if (this.cart.Count <= 0)
            {
                Debug.LogError("Cart is empty");
                return;
            }

            int length = this.cart.Count;
            SteamItemDef_t[] itemDefs = new SteamItemDef_t[length];
            uint[] quantities = new uint[length];

            for (int index = 0; index < length; index++)
            {
                SteamCartItem item = this.cart[index];
                itemDefs[index] = item.ItemDef;
                quantities[index] = item.Quantity;
            }

            SteamInventory.StartPurchase(itemDefs, quantities, (uint)length);
        }

        private void AddToCart(SteamItemDef_t inventoryDef, uint quantity)
        {
            this.cart.Add(new SteamCartItem { ItemDef = inventoryDef, Quantity = quantity });
        }

        public void RemoveFromCart(SteamCartItem steamCartItem)
        {
            if (this.cart.Contains(steamCartItem))
                this.cart.Remove(steamCartItem);
        }

        public bool IsOverlayActive()
        {
            return SteamUtils.IsOverlayEnabled();
        }

        public void OpenOverlay()
        {
            SteamFriends.ActivateGameOverlay("Community");
        }

#endif
    }
}
