
namespace ModIO
{
    /// <summary>
    /// Represents a particular category of tags.
    /// </summary>
    /// <seealso cref="ModIOUnity.GetTagCategories"/>
    /// <seealso cref="ModIOUnityAsync.GetTagCategories"/>
    /// <seealso cref="Tag"/>
    [System.Serializable]
    public struct TagCategory
    {
        public string name;
        public Tag[] tags;
        public bool multiSelect;
        public bool hidden;
        public bool locked;
    }
}
