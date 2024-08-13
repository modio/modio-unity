namespace ModIO.Implementation.API.Objects
{
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
