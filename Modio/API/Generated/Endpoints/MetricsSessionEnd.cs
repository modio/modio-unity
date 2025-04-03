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
        public static partial class Metrics
        {
            /// <summary>Indicates a metrics playtime session has ended. Successful request will return a `204 - No Content` response.</summary>
            internal static async Task<(Error error, JToken response204)> MetricsSessionEndAsJToken(
                MetricsSessionRequest sessionRequest
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/metrics/sessions/end", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.String,"application/json");

                request.Options.AddBody(sessionRequest, "application/json");

                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson(request);
            }

            /// <summary>Indicates a metrics playtime session has ended. Successful request will return a `204 - No Content` response.</summary>
            /// <param name="xModioPlatform">The platform the request is targeting.</param>
            /// <param name="xModioPortal">The portal the request is targeting.</param>
            internal static async Task<(Error error, Response204? response204)> MetricsSessionEnd(
                MetricsSessionRequest sessionRequest
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/metrics/sessions/end", ModioAPIRequestMethod.Post, ModioAPIRequestContentType.String,"application/json");

                request.Options.AddBody(sessionRequest, "application/json");

                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson<Response204>(request);
            }
        }
    }
}
