using UnityEngine;

namespace ModIO.Util
{
    public interface ISimpleMonoSingleton
    {
        void SetupSingleton();
    }

    public class SelfInstancingMonoSingleton<T> : MonoBehaviour, ISimpleMonoSingleton where T : MonoBehaviour
    {
        protected static T _instance;
        public static T Instance
        {
            get {
                #if UNITY_EDITOR
                if(!Application.isPlaying)
                {
                    throw new UnityException($"Attempted to get singleton when application was not playing (This can happen when exiting Playmode. It's safe to ignore)");
                }
                #endif
                if(_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));

                    if(_instance != null)
                    {
                        return _instance;
                    }
                    var go = new GameObject();
                    _instance = go.AddComponent<T>();
                    go.name = _instance.ToString();
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
            Destroy(_instance.gameObject);
            Destroy(gameObject);
            _instance = null;
        }

        public static bool SingletonIsInstantiated() => _instance != null;
    }
}
