using System.Collections.Generic;
using System.Threading.Tasks;
using Modio;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Authentication;
using Modio.Users;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

namespace Plugins.Modio.Unity.Platforms.Android
{
    public class GoogleGamesAuthService : IModioAuthService,
                                          IGetActiveUserIdentifier
    {
#if UNITY_ANDROID
        static readonly List<AuthScope> RequiredAuthScopes = new List<AuthScope>
        {
            AuthScope.EMAIL,
            AuthScope.PROFILE,
            AuthScope.OPEN_ID,
        };
#endif
        
        public async Task<Error> Authenticate(bool displayedTerms, string thirdPartyEmail = null)
        {
#if UNITY_ANDROID
            var authCodeTcs = new TaskCompletionSource<AuthResponse>();
            
            PlayGamesPlatform.Instance.RequestServerSideAccess(false,
                                                               RequiredAuthScopes,
                                                               authCodeTcs.SetResult);

            var authResponse = await authCodeTcs.Task;
            var grantedScopes = authResponse.GetGrantedScopes();
            
            foreach (AuthScope requiredScope in RequiredAuthScopes)
            {
                if (!grantedScopes.Contains(requiredScope))
                {
                    ModioLog.Error?.Log($"Missing required auth scope {requiredScope} for GPG auth!");
                    return Error.Unknown;
                }
            }
            
            (Error error, AccessTokenObject? tokenObejct) 
                = await ModioAPI.Authentication.AuthenticateViaGoogle(
                    new GoogleAuthenticationRequest(authResponse.GetAuthCode(), displayedTerms, thirdPartyEmail, 0)
                );

            if (!error)
                User.Current.OnAuthenticated(tokenObejct.Value.AccessToken);

            return error;
#else
            return Error.Unknown;
#endif
        }

        public ModioAPI.Portal Portal => ModioAPI.Portal.Google;

        public Task<string> GetActiveUserIdentifier()
#if UNITY_ANDROID
            => Task.FromResult(PlayGamesPlatform.Instance.localUser.id);
#else
            => Task.FromResult(string.Empty);
#endif
    }
}
