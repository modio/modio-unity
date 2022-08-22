using System;

namespace ModIOBrowser.Implementation
{
    public class Singleton<T> where T : new()
    {
        private static T _instance;

        public static T Instance
        {
            get {
                if(_instance == null)
                {
                    _instance = new T();
                }

                return _instance;
            }

            set {
                if(_instance != null)
                {
                    throw new NotImplementedException();
                }

                _instance = value;
            }
        }
    }
}
