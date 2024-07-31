using System.Threading;

namespace Plugins.mod.io.Runtime.ModIO.Implementation.Classes
{
    internal class SessionData
    {
        public string SessionId;
        public long[] ModIds;
        public int CurrentNonce;
        public CancellationTokenSource HeartbeatCancellationToken;
    }
}
