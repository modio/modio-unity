using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Modio.API.SchemaDefinitions
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public readonly struct Pagination<T>
    {
        public readonly T Data;
        /// <summary>Number of results returned in this request.</summary>
        public readonly long ResultCount;
        /// <summary>Number of results skipped over.</summary>
        public readonly long ResultOffset;
        /// <summary>Maximum number of results returned in the request.</summary>
        public readonly long ResultLimit;
        /// <summary>Total number of results found.</summary>
        public readonly long ResultTotal;

        /** Auto-generated; must include every (case-insensitive) field name. */
        [JsonConstructor]
        internal Pagination(
            T data,
            long resultCount,
            long resultOffset,
            long resultLimit,
            long resultTotal
        ) {
            Data = data;
            ResultCount = resultCount;
            ResultOffset = resultOffset;
            ResultLimit = resultLimit;
            ResultTotal = resultTotal;
        }
    }
}
