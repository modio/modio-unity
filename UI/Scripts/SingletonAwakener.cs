using System.Collections.Generic;
using System.Linq;
using ModIO.Util;
using UnityEngine;

namespace ModIOBrowser
{
    public class SingletonAwakener : MonoBehaviour
    {
        public List<GameObject> singletons;
        
        private void Awake()
        {
            singletons
                .SelectMany(x => x.GetComponentsInChildren<ISimpleMonoSingleton>())
                .ToList()
                .ForEach(x => x.SetupSingleton());
        }
    }
}
