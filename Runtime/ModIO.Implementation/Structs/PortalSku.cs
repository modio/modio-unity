namespace ModIO.Implementation.Platform
{

    public struct PortalSku
    {
        public readonly UserPortal Portal;
        public readonly string Sku;
        public readonly string Name;
        public readonly string FormattedPrice;
        public readonly int Value;

        public PortalSku(
            UserPortal portal,
            string sku,
            string name,
            string formattedPrice,
            int value
        )
        {
            Portal = portal;
            Sku = sku;
            Name = name;
            FormattedPrice = formattedPrice;
            Value = value;
        }
    }
}
