// <auto-generated />
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Modio.API.SchemaDefinitions;
using Modio.Errors;

namespace Modio.API
{
    public static partial class ModioAPI
    {
        public static partial class Stats
        {
            /// <summary>
            /// <p>Get all mod stats for mods of the corresponding game. Successful request will return an array of [Mod Stats Objects](#get-mod-stats).</p>
            /// <p>__NOTE:__ We highly recommend you apply filters to this endpoint to get only the results you need. For more information regarding filtering please see the [filtering](#filtering) section.</p>
            /// </summary>
            /// <param name="filter">Filter to apply to the request.</param>
            internal static async Task<(Error error, JToken modStatsObjects)> GetModsStatsAsJToken(
            GetModsStatsFilter filter
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/stats", ModioAPIRequestMethod.Get, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddFilterParameters(filter);

                return await _apiInterface.GetJson(request);
            }

            /// <summary>
            /// <p>Get all mod stats for mods of the corresponding game. Successful request will return an array of [Mod Stats Objects](#get-mod-stats).</p>
            /// <p>__NOTE:__ We highly recommend you apply filters to this endpoint to get only the results you need. For more information regarding filtering please see the [filtering](#filtering) section.</p>
            /// </summary>
            /// <param name="filter">Filter to apply to the request.</param>
            internal static async Task<(Error error, Pagination<ModStatsObject[]>? modStatsObjects)> GetModsStats(
            GetModsStatsFilter filter
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/stats", ModioAPIRequestMethod.Get, ModioAPIRequestContentType.FormUrlEncoded);

                request.Options.AddFilterParameters(filter);

                return await _apiInterface.GetJson<Pagination<ModStatsObject[]>>(request);
            }
#region Filter
        
            /// <summary>Constructs a filter built for this request type.</summary>
            /// <param name="pageIndex">The search will skip <c>pageIndex * pageSize</c> results and return (up to) the following <see cref="pageSize"/> results.</param>
            /// <param name="pageSize">Limit the number of results returned (100 max).<p>Use <see cref="SetPageIndex"/> to skip results and return later results.</p></param>
            public static GetModsStatsFilter FilterGetModsStats(
                int pageIndex = 0,
                int pageSize = 100
            ) 
            => new GetModsStatsFilter(
                pageIndex, 
                pageSize
            );
            
            /// <summary>
            /// Filter for GetModsStats, see <see cref="Stats.FilterGetModsStats"/>
            /// to construct this filter <br/>
            /// Filtering options:<br/>
            /// </summary>
            public class GetModsStatsFilter : SearchFilter<GetModsStatsFilter>
            {
                internal GetModsStatsFilter(
                    int pageIndex,
                    int pageSize
                ) : base(pageIndex, pageSize) 
                {
                }

                /// <param name="modId">Unique id of the mod.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter ModId(long modId, Filtering condition = Filtering.None)
                {
                    Parameters[$"mod_id{condition.ClearText()}"] = modId;
                    return this;
                }

                /// <param name="modId">An ICollection of Unique id of the mod.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter ModId(ICollection<long> modId, Filtering condition = Filtering.None)
                {
                    Parameters[$"mod_id{condition.ClearText()}"] = modId;
                    return this;
                }
                

                /// <param name="popularityRankPosition">Current ranking by popularity for the corresponding mod.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter PopularityRankPosition(long popularityRankPosition, Filtering condition = Filtering.None)
                {
                    Parameters[$"popularity_rank_position{condition.ClearText()}"] = popularityRankPosition;
                    return this;
                }

                /// <param name="popularityRankPosition">An ICollection of Current ranking by popularity for the corresponding mod.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter PopularityRankPosition(ICollection<long> popularityRankPosition, Filtering condition = Filtering.None)
                {
                    Parameters[$"popularity_rank_position{condition.ClearText()}"] = popularityRankPosition;
                    return this;
                }
                

                /// <param name="popularityRankTotalMods">Global mod count in which `popularity_rank_position` is compared against.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter PopularityRankTotalMods(long popularityRankTotalMods, Filtering condition = Filtering.None)
                {
                    Parameters[$"popularity_rank_total_mods{condition.ClearText()}"] = popularityRankTotalMods;
                    return this;
                }

                /// <param name="popularityRankTotalMods">An ICollection of Global mod count in which `popularity_rank_position` is compared against.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter PopularityRankTotalMods(ICollection<long> popularityRankTotalMods, Filtering condition = Filtering.None)
                {
                    Parameters[$"popularity_rank_total_mods{condition.ClearText()}"] = popularityRankTotalMods;
                    return this;
                }
                

                /// <param name="downloadsTotal">A sum of all modfile downloads for the corresponding mod.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter DownloadsTotal(long downloadsTotal, Filtering condition = Filtering.None)
                {
                    Parameters[$"downloads_total{condition.ClearText()}"] = downloadsTotal;
                    return this;
                }

                /// <param name="downloadsTotal">An ICollection of A sum of all modfile downloads for the corresponding mod.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter DownloadsTotal(ICollection<long> downloadsTotal, Filtering condition = Filtering.None)
                {
                    Parameters[$"downloads_total{condition.ClearText()}"] = downloadsTotal;
                    return this;
                }
                

                /// <param name="subscribersTotal">A sum of all current subscribers for the corresponding mod.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter SubscribersTotal(long subscribersTotal, Filtering condition = Filtering.None)
                {
                    Parameters[$"subscribers_total{condition.ClearText()}"] = subscribersTotal;
                    return this;
                }

                /// <param name="subscribersTotal">An ICollection of A sum of all current subscribers for the corresponding mod.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter SubscribersTotal(ICollection<long> subscribersTotal, Filtering condition = Filtering.None)
                {
                    Parameters[$"subscribers_total{condition.ClearText()}"] = subscribersTotal;
                    return this;
                }
                

                /// <param name="ratingsPositive">Amount of positive ratings.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter RatingsPositive(long ratingsPositive, Filtering condition = Filtering.None)
                {
                    Parameters[$"ratings_positive{condition.ClearText()}"] = ratingsPositive;
                    return this;
                }

                /// <param name="ratingsPositive">An ICollection of Amount of positive ratings.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter RatingsPositive(ICollection<long> ratingsPositive, Filtering condition = Filtering.None)
                {
                    Parameters[$"ratings_positive{condition.ClearText()}"] = ratingsPositive;
                    return this;
                }
                

                /// <param name="ratingsNegative">Amount of negative ratings.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter RatingsNegative(long ratingsNegative, Filtering condition = Filtering.None)
                {
                    Parameters[$"ratings_negative{condition.ClearText()}"] = ratingsNegative;
                    return this;
                }

                /// <param name="ratingsNegative">An ICollection of Amount of negative ratings.</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetModsStatsFilter RatingsNegative(ICollection<long> ratingsNegative, Filtering condition = Filtering.None)
                {
                    Parameters[$"ratings_negative{condition.ClearText()}"] = ratingsNegative;
                    return this;
                }
                
            }
#endregion
        
        }
    }
}
