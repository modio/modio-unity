
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
        public LogoObject logo;
        public string homepage_url;
        public string name;
        public string name_id;
        public string summary;
        public string description;
        public string description_plaintext;
        public string metadata_blob;
        public string profile_url;
        public ModMediaObject media;
        public ModfileObject modfile;
        public ModStatsObject stats;
        public MetadataKVPObject[] metadata_kvp;
        public ModTagObject[] tags;
    }
}
