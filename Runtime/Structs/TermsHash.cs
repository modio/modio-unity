using System;

namespace ModIO
{
    /// <summary>
    /// This is the hash that identifies the TOS. Used to validate the TOS requirement when
    /// attempting to authenticate a user.
    /// </summary>
    /// <seealso cref="TermsOfUse"/>
    /// <seealso cref="ModIOUnity.GetTermsOfUse"/>
    /// <seealso cref="ModIOUnityAsync.GetTermsOfUse"/>
    [Serializable]
    public struct TermsHash
    {
        public string md5hash;
    }
}
