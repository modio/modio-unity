namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct TokenPackObject
    {
        public long id;
        public long token_pack_id;
        public long price;
        public long amount;
        public string portal;
        public string sku;
        public string name;
        public string description;
        public long date_added;
        public long date_updated;
    }
}
