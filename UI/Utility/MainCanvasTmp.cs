#if UNITY_EDITOR
#endif
using UnityEngine;

//REMOVE THIS CLASS

namespace ModIOBrowser.Implementation
{
    class MainCanvasTmp : MonoBehaviour
    {
        public Canvas canvas;
        public static Canvas Canvas;
        private void Awake()
        {
            Canvas = canvas;
        }
    }
}
