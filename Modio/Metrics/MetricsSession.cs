using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Modio.API.SchemaDefinitions;

namespace Modio.Metrics
{
    public class MetricsSession
    {
        readonly long[] _ids;
        
        internal readonly string SessionId;
        internal long SessionOrderId;
        internal bool Active;
        
        public CancellationTokenSource HeartbeatCancellationToken;
        public TaskCompletionSource<bool> HeartbeatCompletionSource;
        
        public MetricsSession(string id, long[] mods)
        {
            SessionId = id;
            _ids = mods;
            SessionOrderId = 2;
        }

        string GetSessionHash(bool includeIds, string sessionTs, string nonce, string secret)
        {
            string message = null;

            if (includeIds) message = string.Join(",", _ids);

            message += $"{sessionTs}{SessionId}{nonce}";

            byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using var hmac = new HMACSHA256(keyBytes);
            byte[] hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        internal MetricsSessionRequest ToRequest(bool includeIds, string secret)
        {
            var sessionNonce = Guid.NewGuid().ToString();

            long unixTimeMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string hash = GetSessionHash(includeIds, unixTimeMilliseconds.ToString(), sessionNonce, secret);

            return new MetricsSessionRequest(
                SessionId,
                unixTimeMilliseconds,
                hash,
                sessionNonce,
                SessionOrderId,
                includeIds ? _ids : null
            );
        }
    }
}
