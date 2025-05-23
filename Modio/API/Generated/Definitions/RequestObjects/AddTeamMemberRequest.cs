// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject]
    internal readonly partial struct AddTeamMemberRequest : IApiRequest
    {
        static readonly Dictionary<string, object> _bodyParameters = new Dictionary<string, object>();

        /// <summary></summary>
        internal readonly string Email;
        /// <summary></summary>
        internal readonly long ToUserId;
        /// <summary></summary>
        internal readonly string Position;
        /// <summary></summary>
        internal readonly long Level;

        /// <param name="email"></param>
        /// <param name="toUserId"></param>
        /// <param name="position"></param>
        /// <param name="level"></param>
        [JsonConstructor]
        public AddTeamMemberRequest(
            string email,
            long to_user_id,
            string position,
            long level
        ) {
            Email = email;
            ToUserId = to_user_id;
            Position = position;
            Level = level;
        }

        public IReadOnlyDictionary<string, object> GetBodyParameters()
        {
            _bodyParameters.Clear();

            _bodyParameters.Add("email", Email);
            _bodyParameters.Add("to_user_id", ToUserId);
            _bodyParameters.Add("position", Position);
            _bodyParameters.Add("level", Level);

            return _bodyParameters;
        }
    }
}
