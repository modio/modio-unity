using System.Collections.Generic;
using Newtonsoft.Json;

namespace Modio.API.SchemaDefinitions
{
    public readonly partial struct AppleSyncEntitlementsRequestBody : IApiRequest
    {
        static readonly Dictionary<string, object> _bodyParameters = new Dictionary<string, object>();

        internal readonly string Receipt;
        
        /// <param name="idToken"></param>
        [JsonConstructor]
        public AppleSyncEntitlementsRequestBody(
            string receipt
        ) {
            Receipt = receipt;
        }

        public IReadOnlyDictionary<string, object> GetBodyParameters()
        {
            _bodyParameters.Clear();

            _bodyParameters.Add("receipt", Receipt);

            return _bodyParameters;
        }
    }
}
