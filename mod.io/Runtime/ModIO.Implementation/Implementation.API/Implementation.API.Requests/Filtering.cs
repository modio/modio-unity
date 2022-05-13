
namespace ModIO.Implementation.API.Requests
{
    internal static class Filtering
    {
        // Filtering
        public const string FullTextSearch = "_q=";
        public const string NotEqualTo = "-not=";
        public const string Like = "-lk=";
        public const string NotLike = "not-lk=";
        public const string In = "-in=";
        public const string NotIn = "-not-in=";
        public const string Max = "-max=";
        public const string Min = "-min=";
        public const string BitwiseAnd = "-bitwise-and=";

        // Sorting
        public const string Ascending = "_sort=";
        public const string Descending = "_sort=-";

        // Pagination
        public const string Limit = "_limit=";
        public const string Offset = "_offset=";
    }
}
