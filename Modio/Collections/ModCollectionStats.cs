using Modio.API.SchemaDefinitions;
using Modio.Ratings;

namespace Modio.Collections
{
    public class ModCollectionStats
    {
        /// <summary>The collection id.</summary>
        public long CollectionId { get; private set; }
        
        /// <summary> The total number of mods contained in this collection </summary>
        public long ModsTotal { get; private set; }
        
        
        /// <summary>The number of downloads today.</summary>
        public long DownloadsToday{ get; private set; }
        /// <summary>The number of unique downloads.</summary>
        public long UniqueDownloads{ get; private set; }
        /// <summary>The total number of downloads.</summary>
        public long Downloads{ get; private set; }
        /// <summary>The total number of followers.</summary>
        public long Followers{ get; private set; }
        /// <summary>The number of positive ratings in the last 30 days.</summary>
        public long RatingsPositive { get; private set; }
        public long RatingsPositive30Days { get; private set; }
        public long RatingsNegative { get; private set; }
        public long RatingsNegative30Days { get; private set; }
        public float? RatingsPercent{ get; private set; }
        
        ModioRating _previousRating;
        
        internal ModCollectionStats(CollectionStatsObject statsObject, ModioRating previousRating)
        {
            CollectionId = statsObject.CollectionId;
            Followers = statsObject.FollowersTotal;
            UniqueDownloads = statsObject.DownloadsUnique;
            DownloadsToday = statsObject.DownloadsToday;
            Downloads = statsObject.DownloadsTotal;
            RatingsPositive = statsObject.RatingsPositive;
            RatingsPositive30Days = statsObject.RatingsPositive30Days;
            RatingsNegative = statsObject.RatingsNegative;
            RatingsNegative30Days = statsObject.RatingsNegative30Days;

            long totalRatings = RatingsPositive + RatingsNegative;
            RatingsPercent = totalRatings > 0 ? (100 * RatingsPositive) / statsObject.RatingsTotal : null;

            ModsTotal = statsObject.ModsTotal;

            _previousRating = previousRating;
        }

        internal void UpdateEstimateFromLocalRatingChange(ModioRating rating)
        {
            if (_previousRating == ModioRating.Negative) RatingsNegative--;
            if (_previousRating == ModioRating.Positive) RatingsPositive--;
            
            if (rating == ModioRating.Negative) RatingsNegative++;
            if (rating == ModioRating.Positive) RatingsPositive++;

            _previousRating = rating;
            
            long totalRatings = RatingsPositive + RatingsNegative;
            RatingsPercent = totalRatings > 0 ? (RatingsPositive * 100) / totalRatings : null;
        }

        internal void UpdatePreviousRating(ModioRating rating)
        {
            _previousRating = rating;
        }
    }
}
