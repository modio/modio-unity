using System;
using UnityEngine;

namespace Modio.Unity.UI
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ShowIfAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        internal readonly string PredicateName;
#endif

        public ShowIfAttribute(string predicateName)
        {
#if UNITY_EDITOR
            PredicateName = predicateName;
#endif
        }
    }
}
