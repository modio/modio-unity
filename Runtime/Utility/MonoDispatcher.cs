using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ModIO.Util
{
    public class MonoDispatcher : SelfInstancingMonoSingleton<MonoDispatcher>
    {
        Thread mainThread;
        readonly ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();

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
                lock( actions )
                {
                    actions.Enqueue(action);
                }
            }
        }

        void Update()
        {
            lock( actions )
            {
                while(actions.Count > 0)
                {
                    if(actions.TryDequeue(out var result))
                    {
                        result();
                    }
                    else
                    {
                        throw new Exception("Failed to dequeue action!");
                    }
                }
            }
        }
    }
}
