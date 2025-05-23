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
        public static partial class FilesMultipartUploads
        {
            /// <summary>Get all upload sessions belonging to the authenticated user for the corresponding mod. Successful request will return an array of [Multipart Upload Part Objects](#get-multipart-upload-sessions). We recommended reading the [filtering documentation](#filtering) to return only the records you want.</summary>
            /// <param name="filter">Filter to apply to the request.</param>
            /// <param name="modId">Mod id</param>
            internal static async Task<(Error error, JToken multipartUploadObjects)> GetMultipartUploadSessionsAsJToken(
long modId
,
            GetMultipartUploadSessionsFilter filter
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/files/multipart/sessions", ModioAPIRequestMethod.Get);


                return await _apiInterface.GetJson(request);
            }

            /// <summary>Get all upload sessions belonging to the authenticated user for the corresponding mod. Successful request will return an array of [Multipart Upload Part Objects](#get-multipart-upload-sessions). We recommended reading the [filtering documentation](#filtering) to return only the records you want.</summary>
            /// <param name="filter">Filter to apply to the request.</param>
            internal static async Task<(Error error, Pagination<MultipartUploadObject[]>? multipartUploadObjects)> GetMultipartUploadSessions(
long modId
,
            GetMultipartUploadSessionsFilter filter
            ) {
                if (!IsInitialized()) return (new Error(ErrorCode.API_NOT_INITIALIZED), null);

                using var request = ModioAPIRequest.New($"/games/{{game-id}}/mods/{modId}/files/multipart/sessions", ModioAPIRequestMethod.Get);

                request.Options.AddFilterParameters(filter);

                return await _apiInterface.GetJson<Pagination<MultipartUploadObject[]>>(request);
            }
        #region Filter
        
            /// <summary>Constructs a filter built for this request type.</summary>
            /// <param name="pageIndex">The search will skip <c>pageIndex * pageSize</c> results and return (up to) the following <see cref="pageSize"/> results.</param>
            /// <param name="pageSize">Limit the number of results returned (100 max).<p>Use <see cref="SetPageIndex"/> to skip results and return later results.</p></param>
            public static GetMultipartUploadSessionsFilter FilterGetMultipartUploadSessions(
                int pageIndex = 0,
                int pageSize = 100
            ) 
            => new GetMultipartUploadSessionsFilter(
                pageIndex, 
                pageSize
            );
            
            /// <summary>
            /// Filter for GetMultipartUploadSessions, see <see cref="FilesMultipartUploads.FilterGetMultipartUploadSessions"/>
            /// to construct this filter <br/>
            /// Filtering options:<br/>
            /// </summary>
            public class GetMultipartUploadSessionsFilter : SearchFilter<GetMultipartUploadSessionsFilter>
            {
                internal GetMultipartUploadSessionsFilter(
                    int pageIndex,
                    int pageSize
                ) : base(pageIndex, pageSize) 
                {
                }

                /// <param name="status">Status of the modfile upload session:<br/><br/>__0__ = Incomplete (default)<br/>__1__ = Pending<br/>__2__ = Processing<br/>__3__ = Completed<br/>__4__ = Cancelled</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetMultipartUploadSessionsFilter Status(long status, Filtering condition = Filtering.None)
                {
                    Parameters[$"status{condition.ClearText()}"] = status;
                    return this;
                }

                /// <param name="status">An ICollection of Status of the modfile upload session:<br/><br/>__0__ = Incomplete (default)<br/>__1__ = Pending<br/>__2__ = Processing<br/>__3__ = Completed<br/>__4__ = Cancelled</param>
                /// <param name="condition"><see cref="Filtering"/></param>
                public GetMultipartUploadSessionsFilter Status(ICollection<long> status, Filtering condition = Filtering.None)
                {
                    Parameters[$"status{condition.ClearText()}"] = status;
                    return this;
                }
                
            }
            #endregion
        
        }
    }
}
