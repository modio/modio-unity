namespace ModIO
{
    public class ModfileDetails
    {
        /// <summary>
        /// ModId of the mod that you wish to upload the modfile to. (Must be assigned)
        /// </summary>
        public ModId? modId;

        /// <summary>
        /// The directory containing all of the files that makeup the mod. The directory and all of
        /// its contents will be compressed and uploaded when submitted via
        /// ModIOUnity.AddModfile.
        /// </summary>
        public string directory;

        /// <summary>
        /// the changelog for this file version of the mod.
        /// </summary>
        public string changelog;

        /// <summary>
        /// The version number of this modfile as a string (eg 0.2.11)
        /// </summary>
        public string version;

        /// <summary>
        /// Your own custom metadata that can be uploaded with the modfile.
        /// </summary>
        /// <remarks>the metadata has a maximum size of 50,000 characters.</remarks>
        public string metadata;
        
        /// <summary>
        /// Required if the filedata parameter is omitted. The UUID of a completed multipart upload session.
        /// </summary>
        public string uploadId = null;

        /// <summary>
        /// Default value is true. Flag this upload as the current release, this will change the modfile field
        /// on the parent mod to the id of this file after upload.
        /// </summary>
        public bool active = true;
        
        /// <summary>
        /// If platform filtering enabled An array containing one or more platforms this file is targeting.
        /// Valid values can be found under the targeting a platform section.
        /// </summary>
        public string[] platforms = null;

    }
}
