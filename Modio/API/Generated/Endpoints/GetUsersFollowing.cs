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
        public static partial class Me
        {
            /// <summary>
            /// <p>Browse users the authenticated user is following.</p>
            /// <p>Successful request will return an array of [User Objects](#user-object).</p>
            /// </summary>
            /// <param name="filter">Filter to apply to the request.</param>
            internal static async Task<(Error error, JToken userObjects)> GetUsersFollowingAsJToken(
                GetUsersFollowingFilter filter
            )
            {
                if (!IsInitialized())
                    return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New(
                    $"/me/following",
                    ModioAPIRequestMethod.Get,
                    ModioAPIRequestContentType.FormUrlEncoded
                );

                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson(request);
            }

            /// <summary>
            /// <p>Browse users the authenticated user is following.</p>
            /// <p>Successful request will return an array of [User Objects](#user-object).</p>
            /// </summary>
            /// <param name="filter">Filter to apply to the request.</param>
            internal static async Task<(Error error, Pagination<UserObject[]>? userObjects)> GetUsersFollowing(
                GetUsersFollowingFilter filter
            )
            {
                if (!IsInitialized())
                    return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New(
                    $"/me/following",
                    ModioAPIRequestMethod.Get,
                    ModioAPIRequestContentType.FormUrlEncoded
                );

                request.Options.AddFilterParameters(filter);
                request.Options.RequireAuthentication();

                return await _apiInterface.GetJson<Pagination<UserObject[]>>(request);
            }

#region Filter

            /// <summary>Constructs a filter built for this request type.</summary>
            /// <param name="pageIndex">The search will skip <c>pageIndex * pageSize</c> results and return (up to) the following <see cref="pageSize"/> results.</param>
            /// <param name="pageSize">Limit the number of results returned (100 max).<p>Use <see cref="SetPageIndex"/> to skip results and return later results.</p></param>
            public static GetUsersFollowingFilter FilterGetUsersFollowing(
                int pageIndex = 0,
                int pageSize = 100
            )
                => new GetUsersFollowingFilter(
                    pageIndex,
                    pageSize
                );

            /// <summary>
            /// Filter for GetUsersFollowing, see <see cref="Me.FilterGetUsersFollowing"/>
            /// to construct this filter <br/>
            /// Filtering options:<br/>
            /// </summary>
            public class GetUsersFollowingFilter : SearchFilter<GetUsersFollowingFilter>
            {
                internal GetUsersFollowingFilter(
                    int pageIndex,
                    int pageSize
                ) : base(pageIndex, pageSize) { }
            }

#endregion
        }
    }
}
