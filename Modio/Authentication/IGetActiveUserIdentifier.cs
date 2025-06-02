using System.Threading.Tasks;

namespace Modio.Authentication
{
    public interface IGetActiveUserIdentifier
    {
        public Task<string> GetActiveUserIdentifier();
    }
}
