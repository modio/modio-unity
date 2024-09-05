#if (UNITY_IOS || UNITY_ANDROID) && MODIO_MOBILE_IAP
using System;
using System.Collections.Generic;
using System.Linq;
using ModIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using Logger = ModIO.Implementation.Logger;

namespace Plugins.mod.io.Platform.Mobile
{
    public static class MobilePurchaseHelper
    {
        const string PayloadJsonKey = "json";
        const string PayloadKey = "payload";

        // All unconsumed receipts
        static readonly Dictionary<string, PurchaseData> Purchases = new Dictionary<string, PurchaseData>();
        public static int PurchasesCount => Purchases.Count;

        public static PurchaseData GetNextPurchase()
        {
            if (Purchases.Count == 0)
                return null;

            var firstPurchase = Purchases.First();
            Purchases.Remove(firstPurchase.Key);
            return firstPurchase.Value;
        }

        public static void CompleteValidation(Entitlement[] entitlements)
        {
            foreach (var entitlement in entitlements)
            {
                if (entitlement.entitlementConsumed && Purchases.TryGetValue(entitlement.transactionId, out PurchaseData data))
                    data.CompleteValidation?.Invoke();
            }
        }

        public static void QueuePurchase(Product product, Action completeValidation)
        {
            //Is purchase already queued?
            if (Purchases.TryGetValue(product.transactionID, out _))
            {
                Logger.Log(LogLevel.Verbose, $"Product {product.definition.id} was not added because it is already in the queue.");
                return;
            }

            JObject receiptObject;
            try
            {
                receiptObject = JObject.Parse(product.receipt);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, $"Receipt parse failed: {e.Message}");
                return;
            }

            bool keyFound = receiptObject.TryGetValue(PayloadKey, out JToken jsonToken);
            if(!keyFound)
            {
                Logger.Log(LogLevel.Error, $"Unable to get key from receipt object: {PayloadKey}");
                return;
            }

            PurchaseData purchaseData = new PurchaseData
            {
                Payload = jsonToken.ToString(),
                Product = product,
                CompleteValidation = completeValidation,
            };

            JObject payloadObject;
            try
            {
                payloadObject = JObject.Parse(purchaseData.Payload);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, $"Payload parse failed: {e.Message}");
                return;
            }

            keyFound = payloadObject.TryGetValue(PayloadJsonKey, out jsonToken);
            if(!keyFound)
            {
                Logger.Log(LogLevel.Error, $"Unable to get key from receipt object: {PayloadJsonKey}");
                return;
            }

            if (purchaseData.Payload != null && Application.platform == RuntimePlatform.Android)
            {
                purchaseData.PayloadJson = jsonToken.ToString();
            }

            Purchases.Add(purchaseData.Product.transactionID, purchaseData);

            //Call sync entitlements after every purchase
            ModIOUnity.SyncEntitlements((syncResult =>
            {
                if (syncResult.result.Succeeded())
                {
                    Logger.Log(LogLevel.Verbose, $"Synced {syncResult.value.Length} purchase(s).");
                }
            }));
        }
    }
}
#endif
