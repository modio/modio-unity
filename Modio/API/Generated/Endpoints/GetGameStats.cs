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
            /// <summary>Get game stats for the corresponding game. Successful request will return a single [Game Stats Object](#game-stats-object).</summary>
            internal static async Task<(Error error, JToken gameStatsObject)> GetGameStatsAsJToken(

            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/stats", ModioAPIRequestMethod.Get, ModioAPIRequestContentType.FormUrlEncoded);


                return await _apiInterface.GetJson(request);
            }

            /// <summary>Get game stats for the corresponding game. Successful request will return a single [Game Stats Object](#game-stats-object).</summary>
            internal static async Task<(Error error, GameStatsObject? gameStatsObject)> GetGameStats(

            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/stats", ModioAPIRequestMethod.Get, ModioAPIRequestContentType.FormUrlEncoded);


                return await _apiInterface.GetJson<GameStatsObject>(request);
            }
        }
    }
}
