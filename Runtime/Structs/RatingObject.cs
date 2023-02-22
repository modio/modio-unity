namespace ModIO.Implementation.API.Objects
{
    /// <summary>
    /// A struct representing all of the information available for a ModDependenciesObject.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetCurrentUserRatings"/>
    /// <seealso cref="ModIOUnityAsync.GetCurrentUserRatings"/>
    [System.Serializable]
    public struct RatingObject
    {
        public uint game_id;//Unique id of the parent game.
        public long mod_id;//Unique id of the mod.
        public int rating;//Type of rating applied: -1 = Negative Rating or 1 = Positive Rating
        public long date_added;//Unix timestamp of date rating was submitted.
    }
}
