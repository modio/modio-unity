using UnityEngine;

namespace ModIOBrowser.Implementation
{
    public static partial class WebBrowser
    {
        #if UNITY_EDITOR || UNITY_STANDALONE
        public static void OpenWebPage(string url)
        {
            Application.OpenURL(url);
        }
        #endif
    }
}
