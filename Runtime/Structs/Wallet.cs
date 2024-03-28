namespace ModIO
{
    /// <summary>
    /// A struct representing the user's wallet and current balance.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetUserWalletBalance"/>
    /// <seealso cref="ModIOUnityAsync.GetUserWalletBalance"/>;
    public struct Wallet
    {
        public string type;
        public string currency;
        public int balance;
    }
}
