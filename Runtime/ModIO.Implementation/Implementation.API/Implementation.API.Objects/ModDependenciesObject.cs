namespace ModIO.Implementation.API.Objects
{
    /// <summary>
    /// A struct representing all of the information available for a ModDependenciesObject.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetModDependencies"/>
    /// <seealso cref="ModIOUnityAsync.GetModDependencies"/>
    [System.Serializable]
    internal struct ModDependenciesObject
    {
        //Unix timestamp of date the dependency was added.
        public int date_added;
        public int dependency_depth;
        public LogoObject logo;
        public int mod_id;
        public ModfileObject modfile;
        public string name;
        public string name_id;

    }
}
