using System.Collections.Generic;
using System.Linq;
using ModIO.Util;
using UnityEngine;

namespace ModIOBrowser
{
    public class SingletonAwakener : MonoBehaviour
    {
        private bool hasAwakened = false;

        public List<GameObject> singletons;
        
        private void Awake()
        {
            AttemptInitilization();
        }

        private void SetupSingletons()
        {
            singletons
                .SelectMany(x => x.GetComponentsInChildren<ISimpleMonoSingleton>())
                .ToList()
                .ForEach(x => x.SetupSingleton());
        }

        public void AttemptInitilization()
        {
            if(!hasAwakened)
            {
                SetupSingletons();
            }
        }
    }
}
