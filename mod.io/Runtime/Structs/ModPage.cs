namespace ModIO
{
    // TODO @Steve are we going to use ModPage or just return an array and the total separate?
    // My only reason for maybe keeping this is when we implement async versions of the interface.
    // It's more sensible to try and have a single return,
    // eg ResultAnd<ModPage> instead of (Result, int, ModProfile[])
    [System.Serializable]
    public struct ModPage
    {
        public ModProfile[] modProfiles;
        public long totalSearchResultsFound;
    }

    // public struct SearchResult<T>
    // {
    //     public T page;
    //     public long totalResult;
    // }
}
