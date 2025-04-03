using Modio.API.SchemaDefinitions;

namespace Modio.Mods
{
    public class ModStats
    {
        public long Subscribers { get; private set; }
        public long Downloads { get; private set; }
        public long RatingsPositive { get; private set; }
        public long RatingsNegative { get; private set; }
        public long RatingsPercent { get; private set;}

        ModRating _previousRating;

        internal ModStats(ModStatsObject statsObject, ModRating previousRating)
        {
            Subscribers = statsObject.SubscribersTotal;
            Downloads = statsObject.DownloadsTotal;
            RatingsPositive = statsObject.RatingsPositive;
            RatingsNegative = statsObject.RatingsNegative;
            RatingsPercent = statsObject.RatingsPercentagePositive;

            _previousRating = previousRating;
        }

        internal void UpdateEstimateFromLocalRatingChange(ModRating rating)
        {
            if (_previousRating == ModRating.Negative) RatingsNegative--;
            if (_previousRating == ModRating.Positive) RatingsPositive--;
            
            if (rating == ModRating.Negative) RatingsNegative++;
            if (rating == ModRating.Positive) RatingsPositive++;

            _previousRating = rating;
            
            long totalRatings = RatingsPositive + RatingsNegative;
            RatingsPercent = totalRatings > 0 ? (RatingsPositive * 100) / totalRatings : 100;
        }

        internal void UpdatePreviousRating(ModRating rating)
        {
            _previousRating = rating;
        }
    }
}
