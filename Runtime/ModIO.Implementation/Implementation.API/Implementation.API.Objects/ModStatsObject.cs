namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct ModStatsObject
    {
        public long mod_id;
        public long popularity_rank_position;
        public long popularity_rank_total_mods;
        public long downloads_today;
        public long downloads_total;
        public long subscribers_total;
        public long ratings_total;
        public long ratings_positive;
        public long ratings_negative;
        public long ratings_percentage_positive;
        public float ratings_weighted_aggregate;
        public string ratings_display_text;
        public long date_expires;
    }
}
