using Modio.API;

namespace Modio.Authentication
{
    public interface IGetPortalProvider
    {
        public ModioAPI.Portal Portal { get; }
    }
}
