// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject]
    internal readonly partial struct MultipartUploadObject 
    {
        /// <summary>A universally unique identifier (UUID) that represents the upload session.</summary>
        internal readonly string UploadId;
        /// <summary>The status of the upload session:<br/><br/>__0__ = Incomplete<br/>__1__ = Pending<br/>__2__ = Processing<br/>__3__ = Complete<br/>__4__ = Cancelled</summary>
        internal readonly long Status;

        /// <param name="uploadId">A universally unique identifier (UUID) that represents the upload session.</param>
        /// <param name="status">The status of the upload session:<br/><br/>__0__ = Incomplete<br/>__1__ = Pending<br/>__2__ = Processing<br/>__3__ = Complete<br/>__4__ = Cancelled</param>
        [JsonConstructor]
        public MultipartUploadObject(
            string upload_id,
            long status
        ) {
            UploadId = upload_id;
            Status = status;
        }
    }
}
