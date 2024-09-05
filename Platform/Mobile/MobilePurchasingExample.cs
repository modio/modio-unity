#if (UNITY_IOS || UNITY_ANDROID) && MODIO_MOBILE_IAP
using System;
using System.Collections.Generic;
using ModIO;
using Plugins.mod.io.Platform.Mobile;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using Logger = ModIO.Implementation.Logger;
using LogLevel = ModIO.LogLevel;

namespace External
{
    /// <summary>
    /// An Example of how to use the Unity Purchasing module for android and ios devices.
    /// </summary>
    public class MobilePurchasingExample : MonoBehaviour, IDetailedStoreListener
    {
        static IStoreController storeController;
        static IExtensionProvider storeExtensionProvider;
        List<ProductCatalogItem> catalogProducts;

        public async void Awake()
        {
            InitializationOptions options = new InitializationOptions();

            // Set environment based on build type
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            options.SetEnvironmentName("test");
#else
            options.SetEnvironmentName("production");
#endif
            Logger.Log(LogLevel.Verbose, "Initializing Unity Services");
            await UnityServices.InitializeAsync(options);
        }

        public void Start()
        {
            ResourceRequest operation = Resources.LoadAsync<TextAsset>("IAPProductCatalog");
            operation.completed += HandleIAPCatalogLoaded;
            Logger.Log(LogLevel.Verbose, "Initialized Unity Services");
        }

        bool IsInitialized()
        {
            return storeController != null && storeExtensionProvider != null;
        }

        //Converts items from the IAP catalog for use by the Unity Purchasing module
        void HandleIAPCatalogLoaded(AsyncOperation operation)
        {
            ResourceRequest request = operation as ResourceRequest;
            Logger.Log(LogLevel.Verbose, $"Loaded Asset: {request?.asset}");

            var catalog = JsonUtility.FromJson<ProductCatalog>((request?.asset as TextAsset)?.text);
            if (catalog.allProducts is List<ProductCatalogItem> productList)
            {
                catalogProducts = productList;
            }
            else
            {
                Logger.Log(LogLevel.Error, "Catalog data is corrupted!");
                return;
            }

            Logger.Log(LogLevel.Verbose, $"Loaded Catalog with {catalog.allProducts.Count} items.");
            ConfigurationBuilder builder;
#if UNITY_IOS
            builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance(AppStore.AppleAppStore));
#elif UNITY_ANDROID
            builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance(AppStore.GooglePlay));
#else
            builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
#endif

            foreach (var p in catalog.allProducts)
            {
                Logger.Log(LogLevel.Verbose, $"Added product {p.id} to builder.");
                builder.AddProduct(p.id, p.type);
            }

            Logger.Log(LogLevel.Verbose, $"UnityPurchasing Initialize called");
            UnityPurchasing.Initialize(this, builder);
        }


        // Method called when store initialization fails

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            storeController = controller;
            storeExtensionProvider = extensions;
            Logger.Log(LogLevel.Verbose, $"OnInitialized IAP Success");
        }

        // Overloaded method called when store initialization fails with message
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Logger.Log(LogLevel.Error, $"OnInitialize IAP Failed - {error}");
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Logger.Log(LogLevel.Error, $"OnInitialize IAP Failed - {error} : {message}");
        }

        public void GetWalletBalance()
        {
            if (!IsInitialized())
            {
                Debug.LogWarning("IAPManager is not initialized.");
                return;
            }

            //We must create a wallet before a user can sync
            ModIOUnity.GetUserWalletBalance(balanceResult =>
            {
                if (balanceResult.result.Succeeded())
                {
                    Logger.Log(LogLevel.Verbose, $"WalletBalance is {balanceResult.value.balance}");
                }
            });
        }

        //attached to a button to initiate a token purchase
        public void Buy1000Tokens()
        {
            if (!IsInitialized())
            {
                Debug.LogWarning("IAPManager is not initialized.");
                return;
            }

            if(catalogProducts.Count == 0)
            {
                Logger.Log(LogLevel.Verbose, "No products found in catalog!");
                return;
            }

            string id = catalogProducts[0].id;
            Product product = storeController.products.WithID(id);
            if (product != null && product.availableToPurchase)
                storeController.InitiatePurchase(product);
        }

        // Method called when InitiatePurchase is completed without an error
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Logger.Log(LogLevel.Verbose, "ProcessPurchase Called");
            if(catalogProducts.Count == 0)
            {
                Logger.Log(LogLevel.Verbose, "No products found in catalog!");
                return PurchaseProcessingResult.Pending;
            }

            if (String.Equals(args.purchasedProduct.definition.id, catalogProducts[0].id, StringComparison.Ordinal))
            {
                //Save the receipt for processing. This also calls sync entitlements.
                MobilePurchaseHelper.QueuePurchase(args.purchasedProduct, ()=>storeController.ConfirmPendingPurchase(args.purchasedProduct));
                Logger.Log(LogLevel.Verbose, string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            }
            else
            {
                Logger.Log(LogLevel.Verbose, string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            }

            return PurchaseProcessingResult.Pending;
        }

        // Method called when purchase fails
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Logger.Log(LogLevel.Error, $"Purchase Failed - {product.definition.id} : {failureReason}");
        }

        // Overloaded method called when purchase fails with description
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Logger.Log(LogLevel.Error, $"Purchase Failed - {product.definition.id} : {failureDescription.message}");
        }
    }
}
#endif
