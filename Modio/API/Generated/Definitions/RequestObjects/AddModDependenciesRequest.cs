// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject]
    internal readonly partial struct AddModDependenciesRequest : IApiRequest
    {
        static readonly Dictionary<string, object> _bodyParameters = new Dictionary<string, object>();

        /// <summary>The dependencies to add to the mod. When `sync` is true and this attribute is ommitted, all dependencies will be removed.</summary>
        internal readonly long[] Dependencies;
        /// <summary>If true, will remove all existing dependencies and replace with the new ones provided in the request (if any).</summary>
        internal readonly bool Sync;

        /// <param name="dependencies">The dependencies to add to the mod. When `sync` is true and this attribute is ommitted, all dependencies will be removed.</param>
        /// <param name="sync">If true, will remove all existing dependencies and replace with the new ones provided in the request (if any).</param>
        [JsonConstructor]
        public AddModDependenciesRequest(
            long[] dependencies,
            bool sync
        ) {
            Dependencies = dependencies;
            Sync = sync;
        }

        public IReadOnlyDictionary<string, object> GetBodyParameters()
        {
            _bodyParameters.Clear();

            _bodyParameters.Add("dependencies", Dependencies);
            _bodyParameters.Add("sync", Sync);

            return _bodyParameters;
        }
    }
}
