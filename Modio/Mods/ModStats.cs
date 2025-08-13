using Modio.API.SchemaDefinitions;
using Plugins.Modio.Modio.Ratings;

namespace Modio.Mods
{
    public class ModStats
    {
        public long Subscribers { get; private set; }
        public long Downloads { get; private set; }
        public long RatingsPositive { get; private set; }
        public long RatingsNegative { get; private set; }
        public long RatingsPercent { get; private set;}

        ModioRating _previousRating;

        internal ModStats(ModStatsObject statsObject, ModioRating previousRating)
        {
            Subscribers = statsObject.SubscribersTotal;
            Downloads = statsObject.DownloadsTotal;
            RatingsPositive = statsObject.RatingsPositive;
            RatingsNegative = statsObject.RatingsNegative;
            RatingsPercent = statsObject.RatingsPercentagePositive;

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
            RatingsPercent = totalRatings > 0 ? (RatingsPositive * 100) / totalRatings : 100;
        }

        internal void UpdatePreviousRating(ModioRating rating)
        {
            _previousRating = rating;
        }
    }
}
