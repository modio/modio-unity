using UnityEngine;

namespace ModIOBrowser.Implementation
{
    public class SimpleMonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;
        public static T Instance
        {
            get {
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
    }
}
