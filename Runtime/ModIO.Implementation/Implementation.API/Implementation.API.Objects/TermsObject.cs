namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct TermsObject
    {
        public string plaintext;
        public string html;
        public TermsButtonObject buttons;
        public TermsLinksObject links;
    }
}
