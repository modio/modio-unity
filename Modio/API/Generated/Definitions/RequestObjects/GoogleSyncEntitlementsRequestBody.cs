using System.Collections.Generic;
using Newtonsoft.Json;

namespace Modio.API.SchemaDefinitions
{
    public readonly partial struct GoogleSyncEntitlementsRequestBody : IApiRequest
    {
        static readonly Dictionary<string, object> _bodyParameters = new Dictionary<string, object>();

        internal readonly string Receipt;
        
        /// <param name="idToken"></param>
        /// <param name="termsAgreed"></param>
        /// <param name="dateExpires"></param>
        [JsonConstructor]
        public GoogleSyncEntitlementsRequestBody(
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
