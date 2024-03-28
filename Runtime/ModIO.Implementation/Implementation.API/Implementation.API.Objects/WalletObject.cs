namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    public class WalletObject
    {
        public string type;
        public string payment_method_id;
        public string currency;
        public int balance;
    }
}
