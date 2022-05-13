using System;
using System.Collections.Generic;
using System.Threading;
using ModIO.Util;

namespace ModIOBrowser.Implementation
{
    class UnityDispatcher : SelfInstancingMonoSingleton<UnityDispatcher>
    {
        private static Thread mainThread;
        private static object lockItem = new object();
        private static readonly Queue<Action> _actions = new Queue<Action>();

        protected override void Awake()
        {
            base.Awake();
            mainThread = Thread.CurrentThread;
        }

        public static bool MainThread() => Thread.CurrentThread == mainThread;

        public void InvokeAsync(Action action)
        {            
            if(MainThread())
            {
                action();
            }
            else
            {
                lock(lockItem)
                {
                    _actions.Enqueue(action);
                }
            }
        }

        void Update()
        {
            lock(lockItem)
            {
                while(_actions.Count > 0)
                {
                    _actions.Dequeue()();
                }
            }
        }
    }
}
