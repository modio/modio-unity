using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Errors;
using Modio.Monetization;
using Modio.Users;
using Newtonsoft.Json.Linq;

#if MODIO_MOBILE_IAP
using UnityEngine;
using UnityEngine.Purchasing;
#endif

namespace Modio.Unity.Platforms.MobilePurchasing
{
    public class ModioMobileStoreService : IModioVirtualCurrencyProviderService
    {
        const string PAYLOAD_JSON_KEY = "json";
        const string PAYLOAD_KEY = "Payload";
        
        public async Task<(Error error, PortalSku[] skus)> GetCurrencyPackSkus()
        {
            // We reference code dependant on an optional Unity module, hence we wrap it in an ifdef
#if MODIO_MOBILE_IAP
            if (!ModioServices.TryResolve(out IStoreController storeController))
            {
                ModioLog.Error?.Log($"No {nameof(IStoreController)} bound! Cannot get mobile SKUs!");
                return (new Error(ErrorCode.NOT_INITIALIZED), Array.Empty<PortalSku>());
            }

            (Error error, Pagination<GameTokenPackObject[]>? tokenPacks) =
                await ModioAPI.Monetization.GetGameTokenPacks();

            if (error)
            {
                if (!error.IsSilent)
                    ModioLog.Error?.Log($"Error getting product list from mod.io! Please see request error for more info.");
                return (error, Array.Empty<PortalSku>());
            }

            var skuList = new List<PortalSku>();
            
            foreach (Product product in storeController.products.all)
            {
                if (!product.availableToPurchase)
                    continue;
                
                var matchingPack = tokenPacks.Value.Data.FirstOrDefault(pack => pack.Sku == product.definition.id);

                if (matchingPack.Amount <= 0)
                {
                    ModioLog.Message?.Log($"No mod.io Token Pack found that matches SKU {product.definition.id}. Skipping SKU.");
                    continue;
                }

                var result = new PortalSku(
                    ModioAPI.CurrentPortal,
                    product.definition.id,
                    product.metadata.localizedTitle,
                    product.metadata.localizedPrice.ToString(CultureInfo.InvariantCulture),
                    (int)matchingPack.Amount
                );
                
                skuList.Add(result);
            }

            return (Error.None, skuList.ToArray());
#else
            return (Error.Unknown, Array.Empty<PortalSku>());
#endif
        }

        public async Task<Error> OpenCheckoutFlow(PortalSku sku)
        {
            if (!ModioServices.TryResolve(out IModioMobilePurchaseListenerService purchaseListener))
            {
                ModioLog.Error?.Log($"No {nameof(IModioMobilePurchaseListenerService)} bound! Cannot purchase {sku.Sku}!");
                return new Error(ErrorCode.NOT_INITIALIZED);
            }
            
            bool success = await purchaseListener.InitiatePurchase(sku.Sku);

            return success ? Error.None : Error.Unknown;
        }
        
#if MODIO_MOBILE_IAP
        public async Task ConsumeMobileEntitlement(Product purchasedProduct)
        {
            if (!ModioServices.TryResolve(out IStoreController storeController))
            {
                ModioLog.Error?.Log($"No {nameof(IStoreController)} bound! Cannot consume mobile entitlement!"
                                    + $"Cancelling receipt processing.");
                return;
            }
            
            PurchaseData purchaseData = ConvertProductToPurchaseData(purchasedProduct);

            (Error error, Pagination<EntitlementFulfillmentObject[]>? entitlementFulfillmentObjects) result;
            switch (ModioAPI.CurrentPortal)
            {
                case ModioAPI.Portal.Google:
                    result = await ModioAPI.InAppPurchases.SyncGoogleEntitlements(purchaseData.PayloadJson);
                    break;

                case ModioAPI.Portal.Apple:
                    result = await ModioAPI.InAppPurchases.SyncAppleEntitlement(purchaseData.PayloadRaw);
                    break;

                default:
                    ModioLog.Error?.Log($"Portal {ModioAPI.CurrentPortal} is not a recognized mobile portal! Can't "
                                        + "determine where to send purchase receipt to. Please set a "
                                        + $"{nameof(ModioAPI.Portal)} with {nameof(ModioAPI.SetPortal)}!");
                    return;
            }

            if (result.error)
            {
                ModioLog.Error?.Log($"Error consuming entitlement for product {purchaseData.Product.definition.id}: "
                                    + $"{result.error}\nPayload: {purchaseData.PayloadJson}");
                return;
            }

            storeController.ConfirmPendingPurchase(purchasedProduct);
            await User.Current.SyncWallet();
        }
#endif


        // TODO: Convert into IReceiptToPurchaseDataConverter
#if MODIO_MOBILE_IAP
        static PurchaseData ConvertProductToPurchaseData(Product purchasedProduct)
        {
            // We need to convert the raw receipt string into a JObject for parsing
            JObject receiptObject;
            try
            {
                receiptObject = JObject.Parse(purchasedProduct.receipt);
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log($"Receipt parse failed: {e}");
                ModioLog.Error?.Log($"Receipt with Error: {purchasedProduct.receipt}");
                return null;
            }
            
            if(!receiptObject.TryGetValue(PAYLOAD_KEY, out JToken payloadToken))
            {
                ModioLog.Error?.Log($"Unable to get key from receipt object: {PAYLOAD_KEY}");
                return null;
            }
            
            // We need to convert the JToken into a JObject to get its children
            JObject payloadObject;
            try
            {
                payloadObject = Application.platform == RuntimePlatform.IPhonePlayer
                    ? null
                    : JObject.Parse(payloadToken.ToString());
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log($"Payload parse failed: {e}");
                ModioLog.Error?.Log($"PayloadToken with Error: {payloadToken}");
                return null;
            }

            JToken jsonToken = null;
            if(payloadObject is not null
               && !payloadObject.TryGetValue(PAYLOAD_JSON_KEY, out jsonToken))
            {
                ModioLog.Error?.Log($"Unable to get key from receipt object: {PAYLOAD_KEY}");
                return null;
            }

            // PayloadRaw is for Apple
            // PayloadJson is for Google
            
            var purchaseData = new PurchaseData
            {
                PayloadRaw = payloadToken.ToString(),
                PayloadJson = jsonToken?.ToString() ?? string.Empty,
                Product = purchasedProduct,
            };

            return purchaseData;
        }
#endif
    }
}
