using System.Threading.Tasks;

namespace Modio.Monetization
{
    /// <summary>Use this interface if the target platform supports purchasing Virtual Currency but requires it to display in UI in-game.</summary>
    public interface IModioVirtualCurrencyProviderService
    {
        /// <summary>Retrieve a list of <see cref="UserPortal"/> specific SKUs that can be purchased.</summary>
        public Task<(Error error, PortalSku[] skus)> GetCurrencyPackSkus();
        
        /// <summary>Opens the target platform's checkout flow. This will open a separate UI window outside the game.</summary>
        /// <param name="sku">The SKU being purchased.</param>
        public Task<Error> OpenCheckoutFlow(PortalSku sku);
    }
}
