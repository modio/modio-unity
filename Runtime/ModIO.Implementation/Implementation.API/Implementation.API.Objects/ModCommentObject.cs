namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct ModCommentObject
    {
        public long id;
        public long mod_id;
        public long resource_id;
        public long submitted_by;
        public long date_added;
        public long reply_id;
        public string thread_position;
        public long karma;
        public string content;
    }
}
