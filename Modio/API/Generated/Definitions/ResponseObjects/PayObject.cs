// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject]
    internal readonly partial struct PayObject 
    {
        /// <summary>The transaction id.</summary>
        internal readonly long TransactionId;
        /// <summary>The universally unique ID (UUID) that represents a unique tranasction with the payment gateway.</summary>
        internal readonly string GatewayUuid;
        /// <summary>The gross amount of the purchase in the lowest denomination of currency.</summary>
        internal readonly long GrossAmount;
        /// <summary>The net amount of the purchase in the lowest denomination of currency.</summary>
        internal readonly long NetAmount;
        /// <summary>The platform fee of the purchase in the lowest denomination of currency.</summary>
        internal readonly long PlatformFee;
        /// <summary>The gateway fee of the purchase in the lowest denomination of currency.</summary>
        internal readonly long GatewayFee;
        /// <summary>The state of the transaction that was processed. E.g. CANCELLED, CLEARED, FAILED, PAID, PENDING, REFUNDED.</summary>
        internal readonly string TransactionType;
        /// <summary>The metadata that was given in the transaction.</summary>
        internal readonly JArray Meta;
        /// <summary>The time of the purchase.</summary>
        internal readonly long PurchaseDate;
        /// <summary>The type of wallet that was used for the purchase. E.g. STANDARD_MIO.</summary>
        internal readonly string WalletType;
        /// <summary>The balance of the wallet.</summary>
        internal readonly long Balance;
        /// <summary>The deficit of the wallet.</summary>
        internal readonly long Deficit;
        /// <summary>The payment method id that was used.</summary>
        internal readonly string PaymentMethodId;
        /// <summary>The mod that was purchased.</summary>
        internal readonly ModObject Mod;

        /// <param name="transactionId">The transaction id.</param>
        /// <param name="gatewayUuid">The universally unique ID (UUID) that represents a unique tranasction with the payment gateway.</param>
        /// <param name="grossAmount">The gross amount of the purchase in the lowest denomination of currency.</param>
        /// <param name="netAmount">The net amount of the purchase in the lowest denomination of currency.</param>
        /// <param name="platformFee">The platform fee of the purchase in the lowest denomination of currency.</param>
        /// <param name="gatewayFee">The gateway fee of the purchase in the lowest denomination of currency.</param>
        /// <param name="transactionType">The state of the transaction that was processed. E.g. CANCELLED, CLEARED, FAILED, PAID, PENDING, REFUNDED.</param>
        /// <param name="meta">The metadata that was given in the transaction.</param>
        /// <param name="purchaseDate">The time of the purchase.</param>
        /// <param name="walletType">The type of wallet that was used for the purchase. E.g. STANDARD_MIO.</param>
        /// <param name="balance">The balance of the wallet.</param>
        /// <param name="deficit">The deficit of the wallet.</param>
        /// <param name="paymentMethodId">The payment method id that was used.</param>
        /// <param name="mod">The mod that was purchased.</param>
        [JsonConstructor]
        public PayObject(
            long transaction_id,
            string gateway_uuid,
            long gross_amount,
            long net_amount,
            long platform_fee,
            long gateway_fee,
            string transaction_type,
            JArray meta,
            long purchase_date,
            string wallet_type,
            long balance,
            long deficit,
            string payment_method_id,
            ModObject mod
        ) {
            TransactionId = transaction_id;
            GatewayUuid = gateway_uuid;
            GrossAmount = gross_amount;
            NetAmount = net_amount;
            PlatformFee = platform_fee;
            GatewayFee = gateway_fee;
            TransactionType = transaction_type;
            Meta = meta;
            PurchaseDate = purchase_date;
            WalletType = wallet_type;
            Balance = balance;
            Deficit = deficit;
            PaymentMethodId = payment_method_id;
            Mod = mod;
        }
    }
}
