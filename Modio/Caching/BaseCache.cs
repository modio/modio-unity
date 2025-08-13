using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Modio.API;
using Modio.Collections;

namespace Modio.Caching
{
    /// <summary>
    /// Base class for caching objects in Modio.
    /// </summary>
    /// <typeparam name="TCache">The type of the cache class inheriting from this base class.</typeparam>
    /// <typeparam name="TKey">The type of the key used to identify cached objects.</typeparam>
    /// <typeparam name="TCachedObject">The type of the cached objects.</typeparam>
    public abstract class BaseCache<TCache, TKey, TCachedObject>  where TCache : BaseCache<TCache, TKey, TCachedObject>, new() {
        
        static TCache _instance;

        static TCache Instance => _instance ??= new TCache();
        
        /// <summary>
        /// Gets the static cache instance.
        /// </summary>
        internal static Dictionary<TKey, TCachedObject> Cached => Instance._cached;
        
        public static int SearchesNotInCache => Instance._searchesNotInCache;

        public static  int SearchesSavedByCache => Instance._searchesSavedByCache;

        protected BaseCache() => ModioClient.OnShutdown += OnClear;

        class CachedQueryCachedResponse
        {
            internal long ResultTotal { get; private set; }

            internal readonly Dictionary<long, TCachedObject[]> Results = new Dictionary<long, TCachedObject[]>();

            public void AddResults(TCachedObject[] mods, long pageIndex, long resultTotal)
            {
                ResultTotal = resultTotal;

                Results[pageIndex] = mods;
            }
        }
        
        readonly Dictionary<TKey, TCachedObject> _cached = new Dictionary<TKey, TCachedObject>();
        
        
        readonly Dictionary<string, CachedQueryCachedResponse> _cachedSearches = new Dictionary<string, CachedQueryCachedResponse>();
        readonly StringBuilder _stringBuilder = new StringBuilder();
        
        int _searchesNotInCache; 
        int _searchesSavedByCache;
        
        /// <summary>
        /// Gets a cached static object based on the provided key.
        /// </summary>
        /// <param name="key">The key used to identify the cached object.</param>
        /// <returns>Returns the cached object if found, otherwise returns the default value for the cached type.</returns>
        internal static TCachedObject GetCached(TKey key) =>
            Instance.OnGetCached(key);
        
        protected virtual TCachedObject OnGetCached(TKey key)=>
            _cached.TryGetValue(key, out TCachedObject cached)
                ? cached
                : _cached[key] = default(TCachedObject);

        /// <summary>
        /// Tries to get a cached static object based on the provided key.
        /// </summary>
        /// <param name="key">The key used to identify the cached object.</param>
        /// <param name="cached">The cached object if found.</param>
        /// <returns>Returns true if the cached object was found, otherwise false.</returns>
        internal static bool TryGetCachedStatic(TKey key, out TCachedObject cached) =>
            Instance.OnTryGetCached(key, out cached);

        bool OnTryGetCached(TKey key, out TCachedObject cached) => _cached.TryGetValue(key, out cached);

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public static void Clear() => 
            Instance.OnClear();
        
        protected virtual void OnClear()
        {
            _cached.Clear();
            _cachedSearches.Clear();
            _searchesNotInCache = 0;
            _searchesSavedByCache = 0;
        }

        /// <summary>
        /// Gets a cached search result based on the provided filter and search key.
        /// </summary>
        /// <param name="filter">The filter to apply to the search.</param>
        /// <param name="searchKey">The key used to identify the cached search.</param>
        /// <param name="cached">The cached results of the search, if found.</param>
        /// <param name="resultTotal">The total number of results found for the search.</param>
        /// <returns>Returns true if the search results were found in the cache, otherwise false.</returns>
        public static bool GetCachedSearch(
            SearchFilter filter,
            string searchKey,
            out TCachedObject[] cached,
            out long resultTotal
        )  => Instance.OnGetCachedSearch(filter, searchKey, out cached, out resultTotal);
        
        protected virtual bool OnGetCachedSearch(SearchFilter filter, string searchKey, out TCachedObject[] cached, out long resultTotal)
        { 
            if (_cachedSearches.TryGetValue(searchKey, out CachedQueryCachedResponse cachedResponse)
                && cachedResponse.Results.TryGetValue(filter.PageIndex, out cached))
            {
                resultTotal = cachedResponse.ResultTotal;

                _searchesSavedByCache++;

                return true;
            }

            cached = null;
            resultTotal = 0;
            
            _searchesNotInCache++;
            return false;
        }

        /// <summary>
        /// Stores the search results in the cache.
        /// </summary>
        /// <param name="searchKey">The key used to identify the cached search.</param>
        /// <param name="cached">The cached results of the search.</param>
        /// <param name="pageIndex">The index of the page of results.</param>
        /// <param name="resultTotal">The total number of results found for the search.</param>
        public static void CacheSearch(string searchKey, TCachedObject[] cached, long pageIndex, long resultTotal) =>
            Instance.OnCacheSearch(searchKey, cached, pageIndex, resultTotal);
        protected virtual void OnCacheSearch(string searchKey, TCachedObject[] cached, long pageIndex, long resultTotal)
        {
            if (!_cachedSearches.TryGetValue(searchKey, out CachedQueryCachedResponse cachedResponse)) 
                _cachedSearches[searchKey] = cachedResponse = new CachedQueryCachedResponse();
            cachedResponse.AddResults(cached, pageIndex, resultTotal);
        }

        /// <summary>
        /// Clears the cached search results.
        /// </summary>
        public static void ClearCachedSearchCache() => 
            Instance.OnClearCachedSearchCache();
        
        protected virtual void OnClearCachedSearchCache()
        {
            _cachedSearches.Clear();
            _searchesNotInCache = 0;
            _searchesSavedByCache = 0;
        }
        
        /// <summary>
        /// Constructs a filter key based on the provided search filter.
        /// </summary>
        /// <param name="filter">The filter to construct the key from.</param>
        /// <returns>A string representing the constructed filter key.</returns>
        public static string ConstructFilterKey(SearchFilter filter) =>
            Instance.OnConstructFilterKey(filter);

        protected virtual string OnConstructFilterKey(SearchFilter filter)
        { 
            _stringBuilder.Clear();

            _stringBuilder.Append("pageSize:");
            _stringBuilder.Append(filter.PageSize);
            _stringBuilder.Append(",index:");
            _stringBuilder.Append(filter.PageIndex);

            foreach (KeyValuePair<string, object> parameter in filter.Parameters)
                if (!(parameter.Value is string) && parameter.Value is IEnumerable enumerable)
                {
                    _stringBuilder.AppendFormat(",{0}:[", parameter.Key);
                    var first = true;
                    foreach (object o in enumerable)
                    {
                        if (!first)
                            _stringBuilder.Append(',');
                        first = false;
                        _stringBuilder.Append(o);
                    }

                    _stringBuilder.Append(']');
                }
                else
                    _stringBuilder.AppendFormat(",{0}:{1}", parameter.Key, parameter.Value);

            var filterKey = _stringBuilder.ToString();
            _stringBuilder.Clear();
            return filterKey;
        }
    }
}
