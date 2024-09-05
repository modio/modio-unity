using System;

namespace ModIO.Implementation.Platform
{
    public interface IModioSsoPlatform
    {
        public void PerformSso(TermsHash? displayedTerms, Action<Result> onComplete, string optionalThirdPartyEmailAddressUsedForAuthentication = null);
    }
}
