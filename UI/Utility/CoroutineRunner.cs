using System.Collections;
using UnityEngine;
using static ModIO.Utility;

namespace ModIOBrowser.Implementation
{
    class CoroutineRunner : SimpleMonoSingleton<CoroutineRunner>
    {
        public Coroutine Run(IEnumerator coroutine) => StartCoroutine(coroutine);        
    }
}
