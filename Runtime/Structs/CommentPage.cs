namespace ModIO
{
    /// <summary>
    /// A struct containing the ModComments and total number of remaining results that can be
    /// acquired with the SearchFilter used in the GetMods request.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetModComments"/>
    /// <seealso cref="ModIOUnityAsync.GetModComments"/>
    [System.Serializable]
    public struct CommentPage
    {
        /// <summary>
        /// The mod profiles retrieved from this pagination request
        /// </summary>
        /// <seealso cref="ModIOUnity.GetModComments"/>
        /// <seealso cref="ModIOUnityAsync.GetModComments"/>
        public ModComment[] CommentObjects;
        
        /// <summary>
        /// the total results that could be found. eg there may be a total of 1,000 comments but
        /// this CommentPage may only contain the first 100, depending on the SearchFilter pagination
        /// settings.
        /// </summary>
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="SearchFilter.SetPageIndex"/>
        /// <seealso cref="SearchFilter.SetPageSize"/>
        /// <seealso cref="ModIOUnity.GetModComments"/>
        /// <seealso cref="ModIOUnityAsync.GetModComments"/>
        public long totalSearchResultsFound;
    }
}
