using System;

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
        public DateTime dateAdded;
    }
}
