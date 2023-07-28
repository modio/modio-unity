using ModIO.Implementation.Wss.Messages.Objects;
using Newtonsoft.Json.Linq;

namespace ModIO.Implementation.Wss.Messages
{
    internal static class WssRequest
    {
        public static WssMessage DeviceLogin()
        {
            return new WssMessage
            {
                operation = WssOperationType.Wss_DeviceLogin,
                context = JToken.FromObject(new WssDeviceLoginRequest
                {
                    // nonce = Guid.NewGuid().ToString()
                })
            };
        }
    }
}
