using Modio.API.SchemaDefinitions;

namespace Modio.Collections
{
    public struct ModCollectionStats
    {
        /// <summary>The collection id.</summary>
        internal long CollectionId { get; private set; }
        /// <summary>The number of downloads today.</summary>
        internal long DownloadsToday{ get; private set; }
        /// <summary>The number of unique downloads.</summary>
        internal long UniqueDownloads{ get; private set; }
        /// <summary>The total number of downloads.</summary>
        internal long Downloads{ get; private set; }
        /// <summary>The total number of followers.</summary>
        internal long Followers{ get; private set; }
        /// <summary>The number of positive ratings in the last 30 days.</summary>
        internal long RatingsPositive30Days{ get; private set; }
        
        internal ModCollectionStats(CollectionStatsObject statsObject)
        {
            CollectionId = statsObject.CollectionId;
            Followers = statsObject.FollowersTotal;
            UniqueDownloads = statsObject.DownloadsUnique;
            DownloadsToday = statsObject.DownloadsToday;
            Downloads = statsObject.DownloadsTotal;
            RatingsPositive30Days = statsObject.RatingsPositive30Days;
        }
        
    }
}
