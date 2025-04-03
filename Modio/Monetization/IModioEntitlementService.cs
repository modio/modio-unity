using System.Threading.Tasks;

namespace Modio.Monetization
{
    public interface IModioEntitlementService
    {
        public Task<Error> SyncEntitlements();
    }
}
