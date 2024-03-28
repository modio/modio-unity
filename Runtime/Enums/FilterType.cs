
namespace ModIO
{
    /// <summary>
    /// Type of search to be used in the SearchFilter
    /// </summary>
    /// <seealso cref="SearchFilter"/>
    public enum FilterType
    {
        FullTextSearch,
        NotEqualTo,
        Like,
        NotLike,
        In,
        NotIn,
        Max,
        Min,
        BitwiseAnd
    }
}
