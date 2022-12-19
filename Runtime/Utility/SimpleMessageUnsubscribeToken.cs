using System;

namespace ModIO
{
    partial class Utility
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
}
