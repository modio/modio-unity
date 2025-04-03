using System;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Errors;

namespace Modio.Users
{
    public class Wallet
    {
        public string Type { get; private set; }
        public string Currency { get; private set; }
        public long Balance { get; private set; }

        internal Wallet()
        {

        }
        

        internal void ApplyDetailsFromWalletObject(WalletObject walletObject)
        {
            Type = walletObject.Type;
            Currency = walletObject.Currency;
            Balance = walletObject.Balance;
        }

        /// <summary>
        /// Updates the wallets balance, used when a mod is purchased.
        /// </summary>
        /// <param name="newBalance">The new wallet balance </param>
        internal void UpdateBalance(long newBalance)
        {
            Balance = newBalance;
        }
    }
}
