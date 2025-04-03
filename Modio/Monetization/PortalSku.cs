using Modio.API;

namespace Modio.Monetization
{
    public struct PortalSku
    {
        public readonly ModioAPI.Portal Portal;
        public readonly string Sku;
        public readonly string Name;
        public readonly string FormattedPrice;
        public readonly int Value;

        public PortalSku(
            ModioAPI.Portal portal,
            string sku,
            string name,
            string formattedPrice,
            int value
        ) {
            Portal = portal;
            Sku = sku;
            Name = name;
            FormattedPrice = formattedPrice;
            Value = value;
        }
    }
}
