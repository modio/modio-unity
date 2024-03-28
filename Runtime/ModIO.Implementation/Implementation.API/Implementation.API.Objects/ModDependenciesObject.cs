namespace ModIO.Implementation.API.Objects
{
    /// <summary>
    /// A struct representing all of the information available for a ModDependenciesObject.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetModDependencies"/>
    /// <seealso cref="ModIOUnityAsync.GetModDependencies"/>
    [System.Serializable]
    public struct ModDependenciesObject
    {
        //Unique id of the mod that is the dependency.
        public int mod_id;
        //The name of the dependency (mod name).
        public string mod_name;
        //Unix timestamp of date the dependency was added.
        public int date_added;
    }
}
