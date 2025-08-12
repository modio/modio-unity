using System.Threading.Tasks;
using Modio.Extensions;
using UnityEngine;

#if MODIO_MOBILE_IAP
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace Modio.Unity.Platforms.MobilePurchasing
{
    /// <summary>
    /// <c>IDetailedStoreListener</c> needs to be implemented by you as only one can be given to <c>UnityPurchasing</c>.
    /// This is an example of how to integrate your implementation with mod.io's.<br/>
    /// The critical parts of integrating with mod.io are:<br/>
    /// <br/>
    /// Binding services the mod.io plugin requires:
    /// <code>
    /// ModioServices.Bind&lt;IStoreController&gt;()
    ///              .FromInstance(controller);
    /// ModioServices.Bind&lt;IModioMobilePurchaseListenerService&gt;()
    ///              .FromInstance(this);
    /// </code>
    /// Sending the receipt to mod.io's mobile purchasing module:
    /// <code>
    /// if (ModioServices.TryResolve(out ModioMobileStoreService mobileService))
    /// {
    ///     mobileService.ConsumeMobileEntitlement(args.purchasedProduct).ForgetTaskSafely();
    /// }
    /// </code>
    /// Initiating a purchase from the mod.io plugin:
    /// <code>
    /// public Task&lt;bool&gt; InitiatePurchase(string productId)
    /// {
    ///     _purchaseTaskCompletionSource = new TaskCompletionSource&lt;bool&gt;();
    ///     _storeController.InitiatePurchase(productId);
    ///     return _purchaseTaskCompletionSource.Task;
    /// }
    /// </code>
    /// </summary>
    /// <remarks>In this example a <see cref="TaskCompletionSource{TResult}"/> is used to communicate to the plugin when
    /// a purchase has been completed. This can be done in any way you like, however it is recommended that you only
    /// return the <c>boolean</c> when the purchase flow has been completed to communicate to the plugin's UI.</remarks>
    public class MobilePurchasingExample : MonoBehaviour,
#if MODIO_MOBILE_IAP
                                           IDetailedStoreListener,
#endif
                                           IModioMobilePurchaseListenerService
    {
#if MODIO_MOBILE_IAP
        static IStoreController _storeController;
        TaskCompletionSource<bool> _purchaseTaskCompletionSource;
#endif

        public async void Awake()
        {
#if MODIO_MOBILE_IAP
            var options = new InitializationOptions();

            // Set environment based on build type
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
            options.SetEnvironmentName("test");
    #else
            options.SetEnvironmentName("production");
    #endif
            ModioLog.Verbose?.Log("Initializing Unity Services");
            await UnityServices.InitializeAsync(options);
            
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

    #if UNITY_ANDROID
            builder.AddProduct("modio_tokens_1000", ProductType.Consumable);
    #elif UNITY_IOS
            builder.AddProduct("1000_modio_tokens_ios", ProductType.Consumable);
    #endif
            UnityPurchasing.Initialize(this, builder);
    #else
            Destroy(this);
#endif
        }

        // The mod.io plugin will call this method to initiate a purchase of a mod.io linked product.
        public Task<bool> InitiatePurchase(string productId)
        {
#if MODIO_MOBILE_IAP
            _purchaseTaskCompletionSource = new TaskCompletionSource<bool>();
            _storeController.InitiatePurchase(productId);
            return _purchaseTaskCompletionSource.Task;
#else
            return false;
#endif
        }

#if MODIO_MOBILE_IAP
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;

            // This is required for the mod.io plugin to hook into this system
            ModioServices.Bind<IStoreController>()
                         .FromInstance(_storeController);
            ModioServices.Bind<IModioMobilePurchaseListenerService>()
                         .FromInstance(this);
            
            ModioLog.Verbose?.Log($"OnInitialized IAP Success");
        }
        
        // Method called when store initialization fails
        public void OnInitializeFailed(InitializationFailureReason error)
        {
            ModioLog.Error?.Log($"OnInitialize IAP Failed - {error}");
        }
        
        // Overload method called when store initialization fails with message
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            ModioLog.Error?.Log($"OnInitialize IAP Failed - {error} : {message}");
        }

        // Method called when InitiatePurchase is completed without an error
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            // You should differentiate between mod.io products & your own here
            ModioLog.Verbose?.Log($"Processing purchase {args.purchasedProduct.definition.id}");

            _purchaseTaskCompletionSource?.SetResult(true);
            
            if (ModioServices.TryResolve(out ModioMobileStoreService mobileService))
            {
                // The wallet can update in the background. If you want the wallet to be updated before the user is
                // returned to the same UI flow then await the below method instead of using ForgetTaskSafely and
                // set the TCS result after this method.
                mobileService.ConsumeMobileEntitlement(args.purchasedProduct).ForgetTaskSafely();
            }

            return PurchaseProcessingResult.Pending;
        }

        // Method called when purchase fails
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            ModioLog.Error?.Log($"Purchase Failed - {product.definition.id} : {failureReason}");
            _purchaseTaskCompletionSource?.SetResult(false);
        }

        // Overloaded method called when purchase fails with description
        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            ModioLog.Error?.Log($"Purchase Failed - {product.definition.id} : {failureDescription.message}");
            _purchaseTaskCompletionSource?.SetResult(false);
        }
#endif
    }
}
