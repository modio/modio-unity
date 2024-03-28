namespace ModIO
{
    [System.Serializable]
    public struct MultipartUpload
    {
        /// <summary>
        /// A universally unique identifier (UUID) that represents the upload session.
        /// </summary>
        public string upload_id;
        /// <summary>
        /// The status of the upload session: 0 = Incomplete, 1 = Pending, 2 = Processing, 3 = Complete, 4 = Cancelled
        /// </summary>
        public int status;
    }
}
