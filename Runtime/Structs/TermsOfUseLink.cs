using System;

namespace ModIO
{
    /// <summary>
    /// Represents a url as part of the TOS. The 'required' field can be used to determine whether or
    /// not it is a TOS requirement to be displayed to the end user when viewing the TOS text.
    /// </summary>
    [Serializable]
    public struct TermsOfUseLink
    {
        public string name;
        public string url;
        public bool required;
    }
}
