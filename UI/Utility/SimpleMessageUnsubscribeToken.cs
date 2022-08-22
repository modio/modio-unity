using System;

namespace ModIOBrowser.Implementation
{
    class SimpleMessageUnsubscribeToken
    {
        public SimpleMessageUnsubscribeToken(Action unsub)
        {
            unsubAction = unsub;
        }

        private Action unsubAction;
        public void Unsubscribe()
        {
            unsubAction();
        }
    }
}
