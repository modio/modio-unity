using System;

namespace Modio.Platforms.Wss
{
    [Serializable]
    internal struct WssLoginSuccess
    {
        public long code;
        public string access_token;
        public long date_expires;
    }
}
