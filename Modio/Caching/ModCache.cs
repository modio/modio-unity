using System.Collections;
using System.Collections.Generic;
using System.Text;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Mods;
using Modio.Monetization;

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
        
        static readonly Dictionary<ModioId, Mod> Mods = new Dictionary<ModioId, Mod>();
        static readonly Dictionary<string, ModQueryCachedResponse> ModSearches = new Dictionary<string, ModQueryCachedResponse>();

        internal static int SearchesNotInCache; 
        internal static int SearchesSavedByCache;
        static readonly StringBuilder StringBuilder = new StringBuilder();

        /// <summary>Avoid using this if at all possible. Use <see cref="TryGetMod"/> if possible.</summary>
        internal static Mod GetMod(ModioId modId) =>
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

        internal static bool TryGetMod(ModioId modId, out Mod mod) =>
            Mods.TryGetValue(modId, out mod);

        static ModCache()
        {
            ModioClient.OnShutdown += Clear;
            ModioAPI.OnCurrentPortalChanged += PortalChanged;
        }

        public static void Clear()
        {
            Mods.Clear();
            ModSearches.Clear();
            SearchesNotInCache = 0;
            SearchesSavedByCache = 0;
        }

        static void PortalChanged(ModioAPI.Portal portal)
        {
            if (ModioServices.TryResolve(out ModioSettings settings)
                && settings.TryGetPlatformSettings(out MonetizationSettings monetizationSettings)
                && monetizationSettings.MonetizationType == ModioMonetizationType.UsdMarketplace)
            {
                foreach (Mod mod in Mods.Values)
                    if (mod.IsMonetized)
                        mod.GetSkuAfterPortalChange();
            }
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
        
        internal static void ClearMod(ModioId modId)
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

        public static void ClearStartingWith(string startsWith)
        {
            List<string> matches = null;
            
            foreach (string key in ModSearches.Keys)
            {
                if(!key.StartsWith(startsWith))
                    continue;
                matches ??= new List<string>();
                matches.Add(key);
            }

            if (matches == null)
                return;

            foreach (string m in matches)
                ModSearches.Remove(m);
        }
    }
}
