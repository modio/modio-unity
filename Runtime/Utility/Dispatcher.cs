using System;
using System.Collections.Generic;
using System.Threading;

namespace ModIO
{
    partial class Utility
    {
        public class Dispatcher : SimpleMonoSingleton<Dispatcher>
        {
            Thread mainThread;
            readonly Queue<Action> actions = new Queue<Action>();

            protected override void Awake()
            {
                base.Awake();
                mainThread = Thread.CurrentThread;
            }

            public bool MainThread() => Thread.CurrentThread == mainThread;

            public void Run(Action action)
            {
                if(MainThread())
                {
                    action();
                }
                else
                {
                    lock(actions)
                    {
                        actions.Enqueue(action);
                    }
                }
            }

            void Update()
            {
                lock(actions)
                {
                    while(actions.Count > 0)
                    {
                        actions.Dequeue()();
                    }
                }
            }
        }
    }
}
