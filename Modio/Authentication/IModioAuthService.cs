using System.Threading.Tasks;
using Modio.API;

namespace Modio.Authentication
{
    public partial interface IModioAuthService
    {
        /// <summary>
        /// Authenticates with the required server 
        /// </summary>
        /// <param name="displayedTerms">The terms that were displayed to the user</param>
        /// <param name="thirdPartyEmail">Optional email address used for authentication</param>
        /// <param name="sync">Optional parameter to indicate if the profile should begin syncing with the server immediately after authentication</param>
        public Task<Error> Authenticate(
            bool displayedTerms,
            string thirdPartyEmail = null,
            bool sync = true
        );
        
        public ModioAPI.Portal Portal { get; }
    }
}
