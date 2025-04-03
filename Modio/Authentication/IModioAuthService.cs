using System.Threading.Tasks;
using Modio.API;

namespace Modio.Authentication
{
    public interface IModioAuthService
    {
        /// <summary>
        /// Authenticates with the required server 
        /// </summary>
        /// <param name="displayedTerms">The terms that were displayed to the user</param>
        /// <param name="onComplete">Is called when authentication is complete (false on failure)</param>
        /// <param name="thirdPartyEmail">Optional email address used for authentication</param>
        public Task<Error> Authenticate(
            bool displayedTerms,
            string thirdPartyEmail = null
        );
        
        public ModioAPI.Portal Portal { get; }
    }
}
