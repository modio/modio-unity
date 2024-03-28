using System;

namespace ModIO
{
    /// <summary>
    /// A struct representing all of the information available for a Rating.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetCurrentUserRatings"/>
    /// <seealso cref="ModIOUnityAsync.GetCurrentUserRatings"/>
    /// <seealso cref="RatingObject"/>
    [Serializable]
    public struct Rating
    {
        public ModId modId;
        public ModRating rating;
        public DateTime dateAdded;
    }
}
