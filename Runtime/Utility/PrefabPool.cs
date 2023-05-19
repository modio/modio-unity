using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO.Util
{
    internal class PrefabPool : SelfInstancingMonoSingleton<PrefabPool>
    {
        public Dictionary<string, List<MonoBehaviour>> pool = new Dictionary<string, List<MonoBehaviour>>();
        public List<GameObject> pooledItems = new List<GameObject>(); //can find by name here

        public T Load<T>(string name) where T : MonoBehaviour
        {
            //TODO! Turn the pooled items into a dictionary if it grows beyond 10 elements or so
            //as the lookup time starts to matter
            var prefab = pooledItems.FirstOrDefault(x => x.name == name);
            if(prefab == null)
            {
                Debug.LogWarning($"Unable to find {name}");
                return null;
            }

            var go = GameObject.Instantiate(prefab);
            go.name = name;

            return go.GetComponent<T>();
        }

        /// <summary>
        /// Fetch an item from the pool.
        /// This creates a new pool for said item if it doesn't already exists.
        /// </summary>
        public T Get<T>(string name) where T : MonoBehaviour
        {
            if(pool.TryGetValue(name, out var list))
            {
                if(list.Count > 0)
                {
                    var obj = list.First() as T;
                    list.RemoveAt(0);
                    obj.gameObject.SetActive(true);

                    return obj;
                }
            }
            else
            {
                pool.Add(name, new List<MonoBehaviour>());
            }

            T item = Load<T>(name);
            return item;
        }

        /// <summary>
        /// Return an item to its pool.
        /// </summary>
        public void Return<T>(string name, T item) where T : MonoBehaviour
        {
            item.transform.SetParent(null);
            item.gameObject.SetActive(false);

            try
            {
                pool[name].Add(item);
            }
            catch(Exception ex)
            {
                Debug.Log($"Error return item {name} exception {ex}");
            }
        }
    }
}
