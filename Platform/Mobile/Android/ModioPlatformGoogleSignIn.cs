using System;

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformGoogleSignIn : ModioPlatform, IModioSsoPlatform
    {
        private string idToken;

        public static void SetAsPlatform(string idToken)
        {
            ActivePlatform = new ModioPlatformGoogleSignIn
            {
                idToken = idToken,
            };
        }
        public void PerformSso(TermsHash? displayedTerms, Action<Result> onComplete, string optionalThirdPartyEmailAddressUsedForAuthentication = null)
        {
#if UNITY_ANDROID && !MODIO_OCULUS
            ModIOUnity.AuthenticateUserViaGoogle(idToken,
                optionalThirdPartyEmailAddressUsedForAuthentication,
                displayedTerms,
                onComplete);
#endif
        }
    }
}
