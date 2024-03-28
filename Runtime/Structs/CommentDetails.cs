namespace ModIO
{
    public class CommentDetails
    {
        /// <summary>Id of the parent comment this comment is replying to (can be 0 if the comment is not a reply).</summary>
        public long replyId;

        /// <summary>Contents of the comment.</summary>
        public string content;

        public CommentDetails(long replyId, string content)
        {
            this.replyId = replyId;
            this.content = content;
        }
    }
}
