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
        /// ModIOUnity.UploadModfile.
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
    }
}
