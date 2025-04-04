// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject]
    internal readonly partial struct ProxyTransformerObject 
    {
        /// <summary>Did the operation succeed?</summary>
        internal readonly bool Success;

        /// <param name="success">Did the operation succeed?</param>
        [JsonConstructor]
        public ProxyTransformerObject(
            bool success
        ) {
            Success = success;
        }
    }
}
