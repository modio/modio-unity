using System.Collections.Generic;
using Modio.API;

namespace Modio.API
{
    public abstract class SearchFilter<T> : SearchFilter where T : SearchFilter<T>
    {
        protected SearchFilter(int pageIndex, int pageSize)
            : base(pageIndex, pageSize) { }

        public T SetPagination(int pageIndex, int pageSize = 100)
        {
            PageIndex = pageIndex;
            PageSize = pageSize;

            return this as T;
        }
    }

    public abstract class SearchFilter
    {
        internal int PageIndex;
        internal int PageSize;

        internal readonly Dictionary<string, object> Parameters;

        protected SearchFilter(int pageIndex, int pageSize)
        {
            Parameters = new Dictionary<string, object>();
            PageIndex = pageIndex;
            PageSize = pageSize;
        }
    }

    /// <summary>
    /// Filtering phrases to be used with Get requests.<br/>
    /// Filtering Options:<br/>
    /// <see cref="None"/> - No filtering applied.<br/>
    /// <see cref="Like"/> - Where the string parameter matches the given value.<br/>
    /// <see cref="Not"/> - Where the parameter does not match the given value.<br/>
    /// <see cref="NotLike"/> - Where the string parameter does not match the given value.<br/>
    /// <see cref="In"/> - Where the supplied list of values appears in the filtered column value.<br/>
    /// <see cref="NotIn"/> - Where the supplied list of values does not appear in the filtered column value.<br/>
    /// <see cref="Max"/> - Where the column value is smaller than or equal to the specified value.<br/>
    /// <see cref="Min"/> - Where the column value is greater than or equal to the specified value.<br/>
    /// <see cref="BitwiseAnd"/> - Where the column value stored as bits within an integer is included within the given integer value.<br/>
    /// </summary>
    public enum Filtering
    {
        /// <summary>No filtering applied, the same as <c>=</c>.</summary>
        None,
        
        /// <summary>Where the string parameter matches the given value.</summary>
        /// <remarks>Supports wildcards. Use <c>*</c> at the start or end of the string value you're filtering for
        /// to use wildcards in your search queries. Currently only works with string parameters.</remarks>
        /// <example><code>
        /// async void GetModsExample()
        /// {
        ///     GetModsFilter = ModioAPI.Mods.FilterGetMods()
        ///         .Tags("armour", Filtering.Like)
        /// 
        ///     // Returns only mods that have the tag "armour"
        /// }
        ///
        /// async void GetModsWildcardExmaple()
        /// {
        ///     GetModsFilter = ModioAPI.Mods.FilterGetMods()
        ///         .Tags("*armour*", Filtering.Like)
        /// 
        ///     // Returns only mods that have a tag that contains the text "armour" in the tag
        /// }
        /// </code></example>
        Like,
        
        /// <summary>Filter for where the parameter does not match the given value.</summary>
        /// <example><code>
        /// async void GetModsExample()
        /// {
        ///     GetModsFilter = ModioAPI.Mods.FilterGetMods()
        ///         .MetadataKvp("damage:9001", Filtering.Not)
        /// 
        ///     // Returns only mods that don't have the Metadata Key Value Pair of Key: damage | Value: 9001
        /// }
        /// </code></example>
        Not,
        
        /// <summary>Filter for where the string parameter does not match the given value.</summary>
        /// <remarks>Supports wildcards. Use <c>*</c> at the start or end of the string value you're filtering for
        /// to use wildcards in your search queries. Currently only works with string parameters.</remarks>
        /// <example><code>
        /// async void GetModsExample()
        /// {
        ///     GetModsFilter = ModioAPI.Mods.FilterGetMods()
        ///         .Tags("armour", Filtering.NotLike)
        /// 
        ///     // Returns only mods that don't have the tag "armour"
        /// }
        ///
        /// async void GetModsWildcardExmaple()
        /// {
        ///     GetModsFilter = ModioAPI.Mods.FilterGetMods()
        ///         .Tags("*armour*", Filtering.NotLike)
        /// 
        ///     // Returns only mods that have don't a tag that contains the text "armour" in the tag
        /// }
        /// </code></example>
        NotLike,
        
        /// <summary>Where the supplied list of values appears in the filtered column value.</summary>
        /// <example><code>
        /// async void GetCommentsExample()
        /// {
        ///     GetModCommentsFilter = ModioAPI.Comments.FilterGetModComments(100)
        ///         .SubmittedBy({ 1738, 4872, 5519 }, Filtering.In)
        /// 
        ///     // Returns only comments on mod 100 made by the users 1738, 4872 and 5519
        /// }
        /// </code></example>
        In,
        
        /// <summary>Where the supplied list of values does not appear in the filtered column value.</summary>
        /// <example><code>
        /// async void GetCommentsExample()
        /// {
        ///     GetModCommentsFilter = ModioAPI.Comments.FilterGetModComments(100)
        ///         .SubmittedBy({ 1738, 4872, 5519 }, Filtering.NotIn)
        /// 
        ///     // Returns only comments on mod 100 not made by the users 1738, 4872 and 5519
        /// }
        /// </code></example>
        NotIn,
        
        /// <summary>Where the column value is smaller than or equal to the specified value.</summary>
        /// <example><code>
        /// async void GetCommentsExample()
        /// {
        ///     GetModCommentsFilter = ModioAPI.Comments.FilterGetModComments(100)
        ///         .Karma(50, Filtering.Max)
        /// 
        ///     // Returns only comments on mod 100 that have 50 or less karma
        /// }
        /// </code></example>
        Max,
        
        /// <summary>Where the column value is greater than or equal to the specified value.</summary>
        /// <example><code>
        /// async void GetCommentsExample()
        /// {
        ///     GetModCommentsFilter = ModioAPI.Comments.FilterGetModComments(100)
        ///         .Karma(50, Filtering.Min)
        /// 
        ///     // Returns only comments on mod 100 that have 50 or more karma
        /// }
        /// </code></example>
        Min,
        
        /// <summary>Where the column value stored as bits within an integer is included within the given integer value.</summary>
        /// <remarks>
        /// Some columns are stored as bits within an integer. Their value depends on the bits selected. For example, suppose a column has 4 options:<br/>
        /// - 1 = Option A<br/>
        /// - 2 = Option B<br/>
        /// - 4 = Option C<br/>
        /// - 8 = Option D<br/>
        /// You can combine any of these options by adding them together which means there are (2 ^ 4 = 16 possible combinations). For example Option A (1) and Option C (4) would be (1 + 4 = 5), Option A (1), Option C (4) and Option D (8) would be (1 + 4 + 8 = 13), all Options together would be (1 + 2 + 4 + 8 = 15).
        /// </remarks>
        /// <example><code>
        /// async void GetMatureModsExample()
        /// {
        ///     // Alcohol = 1
        ///     // Violence = 4
        ///     // By using 5, we filter for mods with either of those bits
        ///     GetModsFilter = ModioAPI.Mods.FilterGetMods()
        ///         .MaturityOption(5, Filtering.BitwiseAnd)
        /// 
        ///     // Returns only mods that feature either alcohol or violence
        /// }
        /// </code></example>
        BitwiseAnd,
    }

    public static class FilteringExtensions
    {
        public static string ClearText(this Filtering filtering) => filtering switch
        {
            Filtering.Like       => "-lk",
            Filtering.Not        => "-not",
            Filtering.NotLike    => "-not-lk",
            Filtering.In         => "-in",
            Filtering.NotIn      => "-not-in",
            Filtering.Max        => "-max",
            Filtering.Min        => "-min",
            Filtering.BitwiseAnd => "-bitwise-and",
            _                    => string.Empty,
        };
    }
}
