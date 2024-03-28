
namespace ModIO
{
    /// <summary>
    /// Category to be used in the SearchFilter for determining how mods should be filtered in a
    /// request.
    /// </summary>
    /// <seealso cref="SearchFilter"/>
    /// <seealso cref="ModIOUnity.GetMods"/>
    /// <seealso cref="ModIOUnityAsync.GetMods"/>
    public enum SortModsBy
    {
        Name,
        Price,
        Rating,
        Popular,
        Downloads,
        Subscribers,
        DateSubmitted
    }
}
