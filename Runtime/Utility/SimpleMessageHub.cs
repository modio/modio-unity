using System;
using System.Collections.Generic;

namespace ModIO.Util
{
    
    //Any message that will be propagated throughout the Simple Message Hub
    //needs to inherit from this interface
    public interface ISimpleMessage { }

    public class SimpleMessageHub : SelfInstancingMonoSingleton<SimpleMessageHub>
    {
        private readonly Dictionary<Type, List<Action<ISimpleMessage>>> dictionary =
            new Dictionary<Type, List<Action<ISimpleMessage>>>();

        private List<ISimpleMessage> threadSafeMessages = new List<ISimpleMessage>();


        /* Here's the general idea:
            * to publish a message, you can write:
            * SimpleMessageHub.Instance.Publish(new MessageLeavingModDetails());
            * 
            * Anything that needs to listen to this, will simply subscribe:
            * SimpleMessageHub.Instance.Subscribe<MessageLeavingModDetails>(x => {
            *  HideMyself(x);
            * });
            * to subscribe to a message
            *
            * If you at any point you figure you'd need to unsubscribe, you could just do:
            * var token = SimpleMessageHub.Instance.Subscribe<MessageLeavingModDetails>(x => {
            *  HideMyself(x);
            * });
            * token.Unsubscribe();
            * 
            * I have made an attempt to also make this "Unity" thread safe, ie:
            * SimpleMessageHub.Instance.PublishThreadSafe(new MessageLeavingModDetails());
            * 
            * All this does is simply run the code on the Unity thread
            * There may be a caveat here where I'm not sure where the publish message resides,
            * which may have consequences. I'll test for this and cover that up.
            * 
            */

        public SimpleMessageUnsubscribeToken Subscribe<T>(Action<T> subscription) where T : class, ISimpleMessage
        {
            var t = typeof(T);
            if(!dictionary.ContainsKey(t))
            {
                dictionary.Add(t, new List<Action<ISimpleMessage>>());
            }

            //This wraps the action so that we can store it, while making sure that
            //the original message type of the subscription is used as intended
            Action<ISimpleMessage> actionWrapper = x => subscription(x as T);
            dictionary[t].Add(actionWrapper);

            return new SimpleMessageUnsubscribeToken(() =>
            {
                if(dictionary.ContainsKey(t))
                {
                    dictionary[t].Remove(actionWrapper);
                }
            });
        }

        public void Publish<T>(T message) where T : class, ISimpleMessage
        {
            var t = typeof(T);
            if(dictionary.ContainsKey(t))
            {
                foreach(var item in dictionary[t])
                {
                    item(message);
                }
            }
        }

        public void PublishThreadSafe<T>(T message) where T : class, ISimpleMessage
        {
            lock(threadSafeMessages)
            {
                threadSafeMessages.Add(message);
            }
        }

        private void Update()
        {
            lock(threadSafeMessages)
            {
                foreach(var m in threadSafeMessages)
                {
                    Publish(m);
                }
                threadSafeMessages.Clear();
            }
        }

        override protected void OnDestroy()
        {
            dictionary.Clear();
        }

        public void ClearTypeSubscriptions<T>()
        {
            if(dictionary.ContainsKey(typeof(T)))
            {
                dictionary[typeof(T)].Clear();
            }
        }
    }

}
