using Modio.API;
using Modio.Images;

namespace Modio.Monetization
{
    public struct PortalSku
    {
        public readonly ModioAPI.Portal Portal;
        public readonly string Sku;
        public readonly string Name;
        public readonly string FormattedPrice;
        public readonly int Value;
        public readonly string Description;
        public ImageReference ImageReference;

        public PortalSku(
            ModioAPI.Portal portal,
            string sku,
            string name,
            string formattedPrice,
            int value,
            string description = "",
            ImageReference imageReference = default
        ) {
            Portal = portal;
            Sku = sku;
            Name = name;
            FormattedPrice = formattedPrice;
            Value = value;
            Description = description;
            ImageReference = imageReference;
        }
        
    }
}
