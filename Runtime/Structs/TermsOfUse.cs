namespace ModIO
{
    /// <summary>
    /// TOS object received from a successful use of ModIOUnity.GetTermsOfUse
    /// This is used when attempting to authenticate via a third party. You must retrieve the TOS
    /// and input it along with an authentication request.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetTermsOfUse"/>
    /// <seealso cref="ModIOUnityAsync.GetTermsOfUse"/>
    /// <seealso cref="ModIOUnity.AuthenticateUserViaDiscord"/>
    /// <seealso cref="ModIOUnity.AuthenticateUserViaGoogle"/>
    /// <seealso cref="ModIOUnity.AuthenticateUserViaGOG"/>
    /// <seealso cref="ModIOUnity.AuthenticateUserViaItch"/>
    /// <seealso cref="ModIOUnity.AuthenticateUserViaOculus"/>
    /// <seealso cref="ModIOUnity.AuthenticateUserViaSteam"/>
    /// <seealso cref="ModIOUnity.AuthenticateUserViaSwitch"/>
    /// <seealso cref="ModIOUnity.AuthenticateUserViaXbox"/>
    [System.Serializable]
    public struct TermsOfUse
    {
        public string termsOfUse;
        public TermsOfUseLink[] links;
        public TermsHash hash;
    }
}
