using System;
using System.Text;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Authentication;
using Modio.Errors;
using Modio.Users;

#if UNITY_IOS
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Interfaces;
#endif

namespace Modio.Unity.Platforms.Ios
{
    public class IosAuthenticationService : IModioAuthService,
                                            IGetActiveUserIdentifier,
                                            IGetPortalProvider
    {
        public async Task<Error> Authenticate(bool displayedTerms, string thirdPartyEmail = null)
        {
#if UNITY_IOS
            if (!ModioServices.TryResolve(out IAppleAuthManager authManager))
            {
                ModioLog.Error?.Log(
                    $"Error authenticating with Apple, {nameof(IAppleAuthManager)} not bound with "
                    + $"ModioServices! Make sure an instance of {nameof(IAppleAuthManager)} is bound "
                    + $"before attempting to authenticate mod.io with Apple!"
                );

                return new Error(ErrorCode.NOT_INITIALIZED);
            }

            // This is for when a user has logged into this app previously, providing a faster experience. It does
            // NOT support getting email like standard login, but should be attempted first.
            var quickLoginArgs = new AppleAuthQuickLoginArgs();

            var quickLoginTcs = new TaskCompletionSource<CredentialResponse>();

            authManager.QuickLogin(
                quickLoginArgs,
                credential => quickLoginTcs.SetResult(ConvertResponseToCredentialResponse(credential)),
                error => quickLoginTcs.SetResult(ConvertResponseToCredentialResponse(null, error))
            );

            CredentialResponse response = await quickLoginTcs.Task;

            if (response.Credential is not null)
            {
                var error = await SubmitAuthRequest(response);
                return error;
            }

            var loginTcs = new TaskCompletionSource<CredentialResponse>();

            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail);

            authManager.LoginWithAppleId(
                loginArgs,
                credential => loginTcs.SetResult(ConvertResponseToCredentialResponse(credential)),
                error => loginTcs.SetResult(ConvertResponseToCredentialResponse(null, error))
            );

            response = await loginTcs.Task;

            if (response.Credential is not null)
            {
                var error = await SubmitAuthRequest(response);
                return error;
            }

            ModioLog.Error?.Log($"{response.Error.LocalizedFailureReason} - {response.Error.LocalizedDescription}");
            return Error.Unknown;

            CredentialResponse ConvertResponseToCredentialResponse(
                ICredential credential = null,
                IAppleError error = null
            )
                => new CredentialResponse
                {
                    Credential = credential,
                    Error = error,
                };
#else
            return Error.Unknown;
#endif
        }

#if UNITY_IOS
        async Task<Error> SubmitAuthRequest(CredentialResponse response)
        {
            try
            {
                var deserializedCredential = response.Credential as IAppleIDCredential;

                string idToken = Encoding.UTF8.GetString(
                    deserializedCredential.IdentityToken,
                    0,
                    deserializedCredential.IdentityToken.Length
                );

                (Error error, AccessTokenObject? accessTokenObject) =
                    await ModioAPI.Authentication.AuthenticateViaApple(
                        new AppleAuthenticationRequest(
                            idToken,
                            deserializedCredential.Email,
                            true,
                            0
                        )
                    );

                if (!error)
                    User.Current.OnAuthenticated(accessTokenObject.Value.AccessToken);

                return error;
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log(e);
                return Error.Unknown;
            }
        }
        
        class CredentialResponse
        {
            public ICredential Credential;
            public IAppleError Error;
        }
#endif

        public Task<string> GetActiveUserIdentifier() => Task.FromResult("user");

        public ModioAPI.Portal Portal => ModioAPI.Portal.Apple;
    }
}
