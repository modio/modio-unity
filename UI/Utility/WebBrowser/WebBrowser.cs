using UnityEngine;

namespace ModIOBrowser.Implementation
{
    public static partial class WebBrowser
    {
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS || UNITY_WSA
        public static void OpenWebPage(string url)
        {
            Application.OpenURL(url);
        }
#endif
    }
}
