using System.Collections;
using ModIO.Util;
using UnityEngine;

namespace ModIOBrowser.Implementation
{
    class CoroutineRunner : SimpleMonoSingleton<CoroutineRunner>
    {
        public Coroutine Run(IEnumerator coroutine) => StartCoroutine(coroutine);        
    }
}
