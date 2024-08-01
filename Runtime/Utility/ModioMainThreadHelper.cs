using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

namespace ModIO.Util
{
    /// <summary>
    /// A slightly more robust and static friendly version of MonoDispatcher
    /// </summary>
    public class ModioMainThreadHelper : MonoBehaviour
    {
        static ModioMainThreadHelper _instance;
        static readonly ConcurrentQueue<Action> PendingActions = new ConcurrentQueue<Action>();

        Thread _mainThread;

        /// <summary>
        /// The action will be called immediately if we're already on the Unity thread,
        /// </summary>
        /// <param name="runOnMainThread"></param>
        public static void Run(Action runOnMainThread)
        {
            EnsureInstance();

            if (!ReferenceEquals(_instance, null) && _instance._mainThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
            {
                //Invoke immediately; we're already on the main thread
                runOnMainThread();
                return;
            }

            PendingActions.Enqueue(runOnMainThread);
        }

        /// <summary>
        /// Call this from the main thread before calling Run(action) later
        /// You could alternately put an instance in the scene yourself
        /// </summary>
        public static void EnsureInstance()
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ModioMainThreadHelper>();
            }
            if (_instance == null)
            {
                _instance = new GameObject(nameof(ModioMainThreadHelper)).AddComponent<ModioMainThreadHelper>();
            }
        }

        void Awake()
        {
            _instance = this;

            _mainThread = Thread.CurrentThread;

            DontDestroyOnLoad(this);
        }

        void Update()
        {
            while (PendingActions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
}
