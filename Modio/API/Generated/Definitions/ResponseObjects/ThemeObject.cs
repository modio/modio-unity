// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal readonly partial struct ThemeObject 
    {
        /// <summary>The primary hex color code.</summary>
        internal readonly string Primary;
        /// <summary>The dark hex color code.</summary>
        internal readonly string Dark;
        /// <summary>The light hex color code.</summary>
        internal readonly string Light;
        /// <summary>The success hex color code.</summary>
        internal readonly string Success;
        /// <summary>The warning hex color code.</summary>
        internal readonly string Warning;
        /// <summary>The danger hex color code.</summary>
        internal readonly string Danger;

        /// <param name="primary">The primary hex color code.</param>
        /// <param name="dark">The dark hex color code.</param>
        /// <param name="light">The light hex color code.</param>
        /// <param name="success">The success hex color code.</param>
        /// <param name="warning">The warning hex color code.</param>
        /// <param name="danger">The danger hex color code.</param>
        [JsonConstructor]
        public ThemeObject(
            string primary,
            string dark,
            string light,
            string success,
            string warning,
            string danger
        ) {
            Primary = primary;
            Dark = dark;
            Light = light;
            Success = success;
            Warning = warning;
            Danger = danger;
        }
    }
}
