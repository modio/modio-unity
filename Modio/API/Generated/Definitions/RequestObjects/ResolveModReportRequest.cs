// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject]
    internal readonly partial struct ResolveModReportRequest : IApiRequest
    {
        static readonly Dictionary<string, object> _bodyParameters = new Dictionary<string, object>();

        /// <summary>The type of the mod report.</summary>
        internal readonly long Type;

        /// <param name="type">The type of the mod report.</param>
        [JsonConstructor]
        public ResolveModReportRequest(
            long type
        ) {
            Type = type;
        }

        public IReadOnlyDictionary<string, object> GetBodyParameters()
        {
            _bodyParameters.Clear();

            _bodyParameters.Add("type", Type);

            return _bodyParameters;
        }
    }
}
