using System;

namespace ModIO.Util
{
    public class SimpleMessageUnsubscribeToken
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
