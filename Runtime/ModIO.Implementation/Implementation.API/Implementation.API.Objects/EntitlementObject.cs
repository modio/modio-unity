namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct EntitlementObject
    {
        public string transaction_id;
        public int transaction_state;
        public string sku_id;
        public bool entitlement_consumed;
        public int entitlement_type;
        public EntitlementDetailsObject details;
    }
}
