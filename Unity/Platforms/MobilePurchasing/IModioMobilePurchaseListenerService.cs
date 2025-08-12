using System.Threading.Tasks;

namespace Modio.Unity.Platforms.MobilePurchasing
{
    public interface IModioMobilePurchaseListenerService
    {
        Task<bool> InitiatePurchase(string productId);
    }
}
