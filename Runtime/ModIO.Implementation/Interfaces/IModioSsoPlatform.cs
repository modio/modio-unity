using System;

namespace ModIO.Implementation.Platform
{
    public interface IModioSsoPlatform
    {
        public void PerformSso(TermsHash? displayedTerms, Action<bool> onComplete, string optionalThirdPartyEmailAddressUsedForAuthentication = null);
    }
}
