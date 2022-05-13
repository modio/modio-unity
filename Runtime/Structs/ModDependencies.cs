using System;

namespace ModIO.Implementation.API.Objects
{
    /// <summary>
    /// A struct representing all of the information available for a Mod's Dependencies.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetModDependencies"/>
    /// <seealso cref="ModIOUnityAsync.GetModDependencies"/>
    /// <seealso cref="ModDependenciesObject"/>
    [Serializable]
    public struct ModDependencies
    {
        public ModId modId;
        public string modName;
        public DateTime dateAdded;
    }
}
