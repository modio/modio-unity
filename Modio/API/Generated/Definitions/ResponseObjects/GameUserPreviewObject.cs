// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject]
    internal readonly partial struct GameUserPreviewObject 
    {
        /// <summary>The previewing user.</summary>
        internal readonly UserObject User;
        /// <summary>The user who invited the previewing user, if the previewer was added manually.</summary>
        internal readonly UserObject UserFrom;
        /// <summary>The URL of the resource that the registrant should be redirect to upon success.</summary>
        internal readonly string ResourceUrl;
        /// <summary>Unix timestamp of the date the user was registered as a previewer.</summary>
        internal readonly long DateAdded;

        /// <param name="user">The previewing user.</param>
        /// <param name="userFrom">The user who invited the previewing user, if the previewer was added manually.</param>
        /// <param name="resourceUrl">The URL of the resource that the registrant should be redirect to upon success.</param>
        /// <param name="dateAdded">Unix timestamp of the date the user was registered as a previewer.</param>
        [JsonConstructor]
        public GameUserPreviewObject(
            UserObject user,
            UserObject user_from,
            string resource_url,
            long date_added
        ) {
            User = user;
            UserFrom = user_from;
            ResourceUrl = resource_url;
            DateAdded = date_added;
        }
    }
}
