using Newtonsoft.Json.Linq;

namespace Modio.Wss.Messages
{
    [System.Serializable]
    public struct WssMessage
    {
        public string operation;
        public JToken context;

        public bool TryGetValue<TOutput>(out TOutput output) where TOutput : struct
        {
            if (context is { } token)
            {
                output = token.ToObject<TOutput>();
                return true;
            }
            output = default(TOutput);
            return false;
        }
    }
}
