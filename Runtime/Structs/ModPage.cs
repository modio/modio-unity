namespace ModIO
{
    /// <summary>
    /// A struct containing the ModProfiles and total number of remaining results that can be
    /// acquired with the SearchFilter used in the GetMods request.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetMods"/>
    /// <seealso cref="ModIOUnityAsync.GetMods"/>
    [System.Serializable]
    public struct ModPage
    {
        /// <summary>
        /// The mod profiles retrieved from this pagination request
        /// </summary>
        /// <seealso cref="ModIOUnity.GetMods"/>
        /// <seealso cref="ModIOUnityAsync.GetMods"/>
        public ModProfile[] modProfiles;
        
        /// <summary>
        /// the total results that could be found. eg there may be a total of 1,000 mod profiles but
        /// this ModPage may only contain the first 100, depending on the SearchFilter pagination
        /// settings.
        /// </summary>
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="SearchFilter.SetPageIndex"/>
        /// <seealso cref="SearchFilter.SetPageSize"/>
        /// <seealso cref="ModIOUnity.GetMods"/>
        /// <seealso cref="ModIOUnityAsync.GetMods"/>
        public long totalSearchResultsFound;
    }
}
