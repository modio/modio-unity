namespace ModIO
{
    /// <summary>
    /// Detailed stats about a Mod's ratings, downloads, subscribers, popularity etc
    /// </summary>
    [System.Serializable]
    public struct ModStats
    {
        public long modId;
        public long popularityRankPosition;
        public long popularityRankTotalMods;
        public long downloadsToday;
        public long downloadsTotal;
        public long subscriberTotal;
        public long ratingsTotal;
        public long ratingsPositive;
        public long ratingsNegative;
        public long ratingsPercentagePositive;
        public float ratingsWeightedAggregate;
        public string ratingsDisplayText;
        public long dateExpires;
    }
}
