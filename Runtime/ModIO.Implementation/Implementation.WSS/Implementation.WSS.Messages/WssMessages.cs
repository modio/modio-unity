namespace ModIO.Implementation.Wss.Messages
{
    [System.Serializable]
    internal struct WssMessages
    {
        public WssMessage[] messages;

        public WssMessages(params WssMessage[] messages)
        {
            this.messages = messages;
        }
    }
}
