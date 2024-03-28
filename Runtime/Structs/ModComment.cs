namespace ModIO
{
    public struct ModComment
    {
        /// <summary>Unique id of the comment.</summary>
        public long id;

        /// <summary>Unique id of the parent mod. This is now deprecated and will be removed in future
        /// API versions, please use resource_id instead.</summary>
        public ModId modId;

        /// <summary>Unique id of the resource.</summary>
        public long resourceId;

        /// <summary>Unique id of the user who posted the comment.</summary>
        public long submittedBy;

        /// <summary>Unix timestamp of date comment was posted.</summary>
        public long dateAdded;

        /// <summary>
        /// Levels of nesting in a comment thread. You should order by this field, to maintain comment
        /// grouping. How it works:
        ///      - The first comment will have the position '01'.
        ///      - The second comment will have the position '02'.
        ///     - If someone responds to the second comment the position will be '02.01'.
        ///      - A maximum of 3 levels is supported.
        /// </summary>
        public string threadPosition;

        /// <summary>Karma received for the comment (can be positive or negative).</summary>
        public long karma;

        public CommentDetails commentDetails;

        public UserProfile userProfile;
    }
}
