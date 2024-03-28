namespace ModIO.Implementation.API.Objects
{
    public struct CheckoutProcess
    {
        public int transactionId;
        public string grossAmount;
        public string platformFee;
        public string tax;
        public long purchaseDate;
        public int netAmount;
        public int gatewayFee;
        public string transactionType;
        public object meta;
        public string walletType;
        public int balance;
        public int deficit;
        public string paymentMethodId;
        public ModProfile modProfile;
    }
}
