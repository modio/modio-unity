using System.Collections.Generic;
using UnityEngine;

namespace ModIOBrowser.Implementation
{
    static class TransformExtensions
    {
        public static string FullPath(this Transform t)
        {
            Transform current = t;
            string output = current.name;

            while(current != null)
            {
                if(current.parent == null)
                {
                    return output;
                }
                output = current.parent.name + "\\" + output;
                current = current.parent;
            }

            return output;
        }
    }
}
