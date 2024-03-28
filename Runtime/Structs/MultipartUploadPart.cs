namespace ModIO
{

    [System.Serializable]
    public struct MultipartUploadPart
    {
        /// <summary>
        /// A universally unique identifier (UUID) that represents the upload session.
        /// </summary>
        public string upload_id;
        /// <summary>
        /// The part number this object represents.
        /// </summary>
        public int part_number;
        /// <summary>
        /// integer	The size of this part in bytes.
        /// </summary>
        public int part_size;
        /// <summary>
        /// integer	Unix timestamp of date the part was uploaded.
        /// </summary>
        public int date_added;
    }
}
