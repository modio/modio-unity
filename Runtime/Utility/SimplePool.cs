using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModIO.Util
{
    public class SimplePool : SelfInstancingMonoSingleton<SimplePool>
    {
        public Dictionary<string, List<MonoBehaviour>> pool = new Dictionary<string, List<MonoBehaviour>>();

        public T Get<T>(string name, Func<T> constructor) where T : MonoBehaviour
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

            T item = constructor();
            return item;

            //Size of the prefab getting messed up when instantiated and set to a canvas with scaling?
            //Use SetParent(transform, false) to fix!
        }

        public void Return<T>(string id, T item) where T : MonoBehaviour
        {
            item.gameObject.SetActive(false);
            item.gameObject.transform.SetParent(null);
            try
            {
                pool[id].Add(item);
            }
            catch(System.Exception ex)
            {
                Debug.Log($"Error return item {item.name} ({id}) exception {ex}");
            }
        }

        public void PrePool<T>(string name, Func<T> constructor, int num) where T : MonoBehaviour
        {
            var list = new List<T>();
            for(int i = 0; i < num; i++)
                list.Add(Get(name, constructor));

            list.ForEach(x => Return(name, x));
        }
    }
}
