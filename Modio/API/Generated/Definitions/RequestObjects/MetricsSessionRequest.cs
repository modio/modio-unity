using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Modio.API.SchemaDefinitions
{
    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    internal readonly partial struct MetricsSessionRequest : IApiRequest
    {
        static readonly Dictionary<string, object> _bodyParameters = new Dictionary<string, object>();

        internal readonly string SessionId;
        internal readonly long SessionTs;
        internal readonly string SessionHash;
        internal readonly string SessionNonce;
        internal readonly long SessionOrderId;
        internal readonly long[] Ids;

        public MetricsSessionRequest(
            string sessionId,
            long sessionTs,
            string sessionHash,
            string sessionNonce,
            long sessionOrderId,
            long[] ids
        )
        {
            SessionId = sessionId;
            SessionTs = sessionTs;
            SessionHash = sessionHash;
            SessionNonce = sessionNonce;
            SessionOrderId = sessionOrderId;
            Ids = ids;
        }

        public IReadOnlyDictionary<string, object> GetBodyParameters()
        {
            _bodyParameters.Clear();

            _bodyParameters.Add("session_id", SessionId);
            _bodyParameters.Add("session_ts", SessionTs);
            _bodyParameters.Add("session_hash", SessionHash);
            _bodyParameters.Add("session_nonce", SessionNonce);
            _bodyParameters.Add("session_order_id", SessionOrderId);
            if (Ids != null) _bodyParameters.Add("ids", Ids);

            return _bodyParameters;
        }
    }
}
