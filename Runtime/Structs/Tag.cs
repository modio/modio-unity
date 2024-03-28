namespace ModIO
{
    /// <summary>
    /// Represents a Tag that can be assigned to a mod.
    /// </summary>
    /// <seealso cref="TagCategory"/>
    /// <seealso cref="ModIOUnity.GetTagCategories"/>
    /// <seealso cref="ModIOUnityAsync.GetTagCategories"/>
    [System.Serializable]
    public struct Tag
    {
        public string name;
        public int totalUses;
    }
}
