using System;
using ModIO.Implementation.API.Objects;

namespace ModIO
{
    /// <summary>
    /// A struct representing all of the information available for a Mod's Dependencies.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetModDependencies"/>
    /// <seealso cref="ModIOUnityAsync.GetModDependencies"/>
    [Serializable]
    public struct ModDependencies
    {
        public ModId modId;
        public string modName;
        public string modNameId;
        public DateTime dateAdded;
        public int dependencyDepth;

        public DownloadReference logoImage_320x180;
        public DownloadReference logoImage_640x360;
        public DownloadReference logoImage_1280x720;
        public DownloadReference logoImageOriginal;

        //TODO: this may not be filled reliably
        public Modfile modfile;
    }
}
