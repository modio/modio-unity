using System.Collections;
using System.Collections.Generic;
using System.Text;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Mods;

namespace Modio.Caching
{
    internal static class ModCache
    {
        class ModQueryCachedResponse
        {
            internal long ResultTotal { get; private set; }

            internal readonly Dictionary<long, Mod[]> Results = new Dictionary<long, Mod[]>();

            public void AddResults(Mod[] mods, long pageIndex, long resultTotal)
            {
                ResultTotal = resultTotal;

                Results[pageIndex] = mods;
            }
        }
        
        static readonly Dictionary<ModId, Mod> Mods = new Dictionary<ModId, Mod>();
        static readonly Dictionary<string, ModQueryCachedResponse> ModSearches = new Dictionary<string, ModQueryCachedResponse>();

        internal static int SearchesNotInCache; 
        internal static int SearchesSavedByCache;
        static readonly StringBuilder StringBuilder = new StringBuilder();

        /// <summary>Avoid using this if at all possible. Use <see cref="TryGetMod"/> if possible.</summary>
        internal static Mod GetMod(ModId modId) =>
            Mods.TryGetValue(modId, out Mod mod)
                ? mod
                : Mods[modId] = new Mod(modId);

        /// <summary>
        /// This is the preferred method if we have a modObject; it will populate the Mod with the data if needed
        /// </summary>
        internal static Mod GetMod(ModObject modObject) =>
            Mods.TryGetValue(modObject.Id, out Mod mod)
                ? mod.ApplyDetailsFromModObject(modObject)
                : Mods[modObject.Id] = new Mod(modObject);

        internal static bool TryGetMod(ModId modId, out Mod mod) =>
            Mods.TryGetValue(modId, out mod);

        static ModCache() => ModioClient.OnShutdown += Clear;

        public static void Clear()
        {
            Mods.Clear();
            ModSearches.Clear();
            SearchesNotInCache = 0;
            SearchesSavedByCache = 0;
        }

        internal static bool GetCachedModSearch(
            SearchFilter filter,
            string searchKey,
            out Mod[] cachedMods,
            out long resultTotal
        )
        {
            if (ModSearches.TryGetValue(searchKey, out ModQueryCachedResponse cachedResponse)
                && cachedResponse.Results.TryGetValue(filter.PageIndex, out cachedMods))
            {
                resultTotal = cachedResponse.ResultTotal;

                SearchesSavedByCache++;

                return true;
            }

            cachedMods = null;
            resultTotal = 0;

            SearchesNotInCache++;
            return false;
        }

        internal static void CacheModSearch(string searchKey, Mod[] mods, long pageIndex, long resultTotal)
        {
            if (!ModSearches.TryGetValue(searchKey, out ModQueryCachedResponse cachedResponse)) 
                ModSearches[searchKey] = cachedResponse = new ModQueryCachedResponse();
            cachedResponse.AddResults(mods, pageIndex, resultTotal);
        }

        internal static void ClearModSearchCache()
        {
            ModSearches.Clear();
            SearchesNotInCache = 0;
            SearchesSavedByCache = 0;
        }
        
        internal static void ClearMod(ModId modId)
        {
            ClearModSearchCache();
            Mods.Remove(modId);
        }

        internal static string ConstructFilterKey(SearchFilter filter)
        {
            StringBuilder.Clear();

            StringBuilder.Append("pageSize:");
            StringBuilder.Append(filter.PageSize);
            StringBuilder.Append(",index:");
            StringBuilder.Append(filter.PageIndex);

            foreach (KeyValuePair<string, object> parameter in filter.Parameters)
                if (!(parameter.Value is string) && parameter.Value is IEnumerable enumerable)
                {
                    StringBuilder.AppendFormat(",{0}:[", parameter.Key);
                    var first = true;
                    foreach (object o in enumerable)
                    {
                        if (!first)
                            StringBuilder.Append(',');
                        first = false;
                        StringBuilder.Append(o);
                    }

                    StringBuilder.Append(']');
                }
                else
                    StringBuilder.AppendFormat(",{0}:{1}", parameter.Key, parameter.Value);

            var filterKey = StringBuilder.ToString();
            StringBuilder.Clear();
            return filterKey;
        }
    }
}
