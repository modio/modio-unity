namespace ModIO
{
    [System.Serializable]
    public struct ModStats
    {
        public ModId modId;
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
    }
}
