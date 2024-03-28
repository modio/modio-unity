namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct CheckoutProcessObject
    {
        public int transaction_id;
        public string gross_amount;
        public string platform_fee;
        public string tax;
        public long purchase_date;
        public int net_amount;
        public int gateway_fee;
        public string transaction_type;
        public object meta;
        public string wallet_type;
        public int balance;
        public int deficit;
        public string payment_method_id;
        public ModObject mod;
    }
}
