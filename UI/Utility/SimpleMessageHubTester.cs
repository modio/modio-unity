using System.Collections;
using UnityEngine;

namespace ModIOBrowser.Implementation
{
    class MessagePoke : ISimpleMessage
    {
        public int number;
    }

    class SimpleMessageHubTester : SimpleMonoSingleton<SimpleMessageHubTester>
    {
        SimpleMessageUnsubscribeToken subToken;

        public void RunTest()
        {
            subToken = SimpleMessageHub.Instance.Subscribe<MessagePoke>(x =>
            {
                Debug.Log($"I got a message! {x.number}");
            });

            StartCoroutine(PokeMessages());
        }

        IEnumerator PokeMessages()
        {
            for(int i = 0; i < 10; i++)
            {
                SimpleMessageHub.Instance.Publish(new MessagePoke()
                {
                    number = i
                });
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("Unsubscribing!");
            subToken.Unsubscribe();

            SimpleMessageHub.Instance.Publish(new MessagePoke()
            {
                number = 99
            });
        }
    }
}
