using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Modio.API.SchemaDefinitions
{
    internal readonly struct CreateModMonetizationTeamRequest : IApiRequest
    {
        static readonly Dictionary<string, object> _bodyParameters = new Dictionary<string, object>();

        internal readonly long[] UserIds;
        internal readonly int[] Splits;

        [JsonConstructor]
        public CreateModMonetizationTeamRequest(
            long[] userIds,
            int[] splits
        ) {
            UserIds = userIds;
            Splits = splits;
        }
        
        public IReadOnlyDictionary<string, object> GetBodyParameters()
        {
            _bodyParameters.Clear();

            for (int i = 0; i < UserIds.Length; i++)
            {
                _bodyParameters.Add($"users[{i}][id]", UserIds[i]);
                _bodyParameters.Add($"users[{i}][split]", Splits[i]);
            }

            return _bodyParameters;
        }
    }
}
