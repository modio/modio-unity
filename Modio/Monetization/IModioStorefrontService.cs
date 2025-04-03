using System.Threading.Tasks;

namespace Modio.Monetization
{
    public interface IModioStorefrontService
    {
        public Task<Error> OpenPlatformPurchaseFlow();
    }
}
