// <auto-generated />
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace Modio.API.SchemaDefinitions{
    [JsonObject]
    internal readonly partial struct ContactRequest : IApiRequest
    {
        static readonly Dictionary<string, object> _bodyParameters = new Dictionary<string, object>();

        /// <summary>The e-mail of the person contacting us.</summary>
        internal readonly string Email;
        /// <summary>The subject of the report. Max 100 characters.</summary>
        internal readonly string Subject;
        /// <summary>The content (body) of the message. Max 50,000 characters.</summary>
        internal readonly string Message;

        /// <param name="email">The e-mail of the person contacting us.</param>
        /// <param name="subject">The subject of the report. Max 100 characters.</param>
        /// <param name="message">The content (body) of the message. Max 50,000 characters.</param>
        [JsonConstructor]
        public ContactRequest(
            string email,
            string subject,
            string message
        ) {
            Email = email;
            Subject = subject;
            Message = message;
        }

        public IReadOnlyDictionary<string, object> GetBodyParameters()
        {
            _bodyParameters.Clear();

            _bodyParameters.Add("email", Email);
            _bodyParameters.Add("subject", Subject);
            _bodyParameters.Add("message", Message);

            return _bodyParameters;
        }
    }
}
