using System.Collections;
using ModIO.Util;
using UnityEngine;

namespace Plugins.mod.io.Runtime.Utility
{
    class CoroutineRunner : SelfInstancingMonoSingleton<CoroutineRunner>
    {
        public Coroutine Run(IEnumerator coroutine) => this.StartCoroutine(coroutine);
    }
}
