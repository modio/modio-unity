namespace ModIO.Implementation.API.Objects
{
    [System.Serializable]
    internal struct ModCommentObject
    {
        public long id;
        public long game_id;
        public UserObject user;
        public long mod_id;
        public long resource_id;
        public long submitted_by;
        public long date_added;
        public long reply_id;
        public string thread_position;
        public long karma;
        public string content;
    }

    internal struct ModComment
    {
        public long id;
        public long gameId;
        public UserObject user;
        public long modId;
        public long resourceId;
        public long submittedBy;
        public long dateAdded;
        public long replyId;
        public string threadPosition;
        public long karma;
        public string content;
    }
}
