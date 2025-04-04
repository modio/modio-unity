// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject(MemberSerialization.Fields)]
    internal readonly partial struct ModPlatformsObject 
    {
        /// <summary>A [target platform](#targeting-a-platform).</summary>
        internal readonly string Platform;
        /// <summary>The unique id of the modfile that is currently live on the platform specified in the `platform` field.</summary>
        internal readonly long ModfileLive;

        /// <param name="platform">A [target platform](#targeting-a-platform).</param>
        /// <param name="modfileLive">The unique id of the modfile that is currently live on the platform specified in the `platform` field.</param>
        [JsonConstructor]
        public ModPlatformsObject(
            string platform,
            long modfile_live
        ) {
            Platform = platform;
            ModfileLive = modfile_live;
        }
    }
}
