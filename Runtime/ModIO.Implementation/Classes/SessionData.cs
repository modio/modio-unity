using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Plugins.mod.io.Runtime.ModIO.Implementation.Classes
{
    internal class SessionData
    {
        public string SessionId;
        public long[] ModIds;
        public int SessionOrderId;

        public CancellationTokenSource HeartbeatCancellationToken;

        public SessionData(string sessionId, long[] modIds)
        {
            SessionId = sessionId;
            ModIds = modIds;
            SessionOrderId = 1;
        }
        
        public string GetSessionHash(bool includeIds, string sessionTs, string nonce)
        {
            string message = null;
            
            if (includeIds)
                message = string.Join(",", ModIds);
            
            message += $"{sessionTs}{SessionId}{nonce}";

            byte[] keyBytes = Encoding.UTF8.GetBytes(AnalyticsManager.MetricsSecret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            using HMACSHA256 hmac = new HMACSHA256(keyBytes);
            byte[] hashBytes = hmac.ComputeHash(messageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
