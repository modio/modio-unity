namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct ModObject
    {
        public long id;
        public long game_id;
        public int status;
        public int visible;
        public UserObject submitted_by;
        public long date_added;
        public long date_updated;
        public long date_live;
        public int maturity_option;
        public int stock;
        public int community_options;
        public int monetisation_options;
        public int price;
        public int tax;
        public LogoObject logo;
        public string homepage_url;
        public string name;
        public string name_id;
        public ModfileObject modfile;
        public string metadata_blob;
        public MetadataKVPObject[] metadata_kvp;
        public ModTagObject[] tags;
        public string platform_status;
        public RevenueType revenue_type;
        public string summary;
        public string description;
        public string description_plaintext;
        public ModStatsObject stats;
        public string profile_url;
        public ModMediaObject media;
        public ModPlatformsObject[] platforms;
    }
}
