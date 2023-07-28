using Newtonsoft.Json.Linq;

namespace ModIO.Implementation.Wss.Messages
{
    [System.Serializable]
    internal struct WssMessage
    {
        public string operation;
        public JToken context;

        public bool TryGetValue<TOutput>(out TOutput output) where TOutput : struct
        {
            if (context is JToken token)
            {
                output = token.ToObject<TOutput>();
                return true;
            }
            output = default;
            return false;
        }
    }
}
