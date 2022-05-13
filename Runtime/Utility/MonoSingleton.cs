using UnityEngine;

namespace ModIO.Util
{
    public class MonoSingleton<T> : MonoBehaviour, ISimpleMonoSingleton where T : MonoBehaviour
    {
        protected static T _instance;
        public static T Instance
        {
            get {
                if(_instance == null)
                {
                    throw new UnityException("This singleton is not instanced. Maybe you need to initiate this code, perhaps via a prefab?");
                }
                
                return _instance;
            }

            private set {
                _instance = value;
            }
        }

        protected virtual void Awake()
        {
            SetupSingleton();
        }

        public void SetupSingleton()
        {
            if(_instance != null && _instance != this)
            {
                Debug.Log($"Instance of {gameObject.name} already exists on awake, killing.");
                Destroy(_instance.gameObject);
                _instance = null;
            }

            _instance = this as T;
            _instance.name = this.ToString();
        }

        protected virtual void OnDestroy()
        {
            _instance = null;
        }

        protected virtual void OnApplicationQuit()
        {
            Destroy(gameObject);
            _instance = null;
        }

        public static bool SingletonIsInstantiated() => _instance != null;
    }
}
