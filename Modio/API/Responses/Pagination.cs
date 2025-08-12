using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modio.Users;
using Newtonsoft.Json;
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
        
        /// <summary>
        /// Crawls all pages
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="method"></param>
        /// <typeparam name="TFilter">The Filter type to use for this Crawl</typeparam>
        /// <typeparam name="TObject">The Type of object expected from the crawl</typeparam>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="List{T}"/> results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>results</c> is a list of the expected type users.</p>
        /// </returns>
        internal static async Task<(Error error, List<TObject> results)> CrawlAllPages<TFilter, TObject>(
            TFilter filter, 
            Func<TFilter, Task<(Error error, Pagination<TObject[]>?)>> method
        ) 
            where TFilter : SearchFilter<TFilter>
        {
            var output = new List<TObject>();
            
            while (true)
            {
                (Error error, Pagination<TObject[]>? result) 
                    = await method(filter);

                if (error)
                {
                    if (!error.IsSilent) 
                        ModioLog.Warning?.Log($"Error crawling pages for user {User.Current.UserId}: {error.GetMessage()}");
                    return (error, new List<TObject>());
                }

                output.AddRange(result.Value.Data);

                // Check for if we're on the last page, if not we increment the page number and iterate
                if (result.Value.ResultOffset + result.Value.ResultCount < result.Value.ResultTotal)
                    filter.PageIndex++;
                else
                    break;
            }

            return (Error.None, output);
        }

        /// <summary>
        /// Crawls all pages
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="method"></param>
        /// <typeparam name="TFilter">The Filter type to use for this Crawl</typeparam>
        /// <typeparam name="TObject">The Type of object expected from the crawl</typeparam>
        /// <typeparam name="TKey">The Type of key to use for this Crawl</typeparam>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="List{T}"/> results), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>results</c> is a list of the expected type users.</p>
        /// </returns>
        internal static async Task<(Error error, List<TObject> results)> CrawlAllPages<TFilter, TKey, TObject>(
            TFilter filter,
            TKey key,
            Func<TKey,TFilter, Task<(Error error, Pagination<TObject[]>?)>> method
        ) 
            where TFilter : SearchFilter<TFilter>
        {
            var output = new List<TObject>();
            
            while (true)
            {
                (Error error, Pagination<TObject[]>? result) 
                    = await method(key, filter);

                if (error)
                {
                    if (!error.IsSilent) 
                        ModioLog.Warning?.Log($"Error crawling pages for user {User.Current.UserId}: {error.GetMessage()}");
                    return (error, new List<TObject>());
                }

                output.AddRange(result.Value.Data);

                // Check for if we're on the last page, if not we increment the page number and iterate
                if (result.Value.ResultOffset + result.Value.ResultCount < result.Value.ResultTotal)
                    filter.PageIndex++;
                else
                    break;
            }

            return (Error.None, output);
        }
    }
}
