using System;
using UnityEngine;

namespace Modio.Unity
{
    /// <summary>
    /// You do not need to use this class. This is used to ensure specific types in API request
    /// objects and anything we serialize gets AOT code generated when using IL2CPP compilation.
    /// </summary>
    internal class AotTypeEnforcer : MonoBehaviour
    {
        void Awake()
        {
            API.AotTypeEnforcer.Hello();
        }
    }
}
