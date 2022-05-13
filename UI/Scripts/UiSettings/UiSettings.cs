using ModIOBrowser.Implementation;
using UnityEngine;

namespace ModIOBrowser
{
    [CreateAssetMenu(fileName = "UiSettings.asset", menuName = "ModIo/UiSettings")]
    public partial class UiSettings : ScriptableObject
    {
	    public bool StandaloneUsesVKDelegate = true;
	    public bool AndroidUsesVKDelegate = false;
	    public bool IOSUsesVKDelegate = false;

        public TranslatedLanguages Language = TranslatedLanguages.English;

        [HideInInspector]
        public GlyphPlatforms GlyphPlatform;

        [Range(0f, 1f)]
        public float volume = 1f;

#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WSA
	    public bool ShouldWeUseVirtualKeyboardDelegate() => StandaloneUsesVKDelegate;
#elif !UNITY_EDITOR && UNITY_ANDROID
	    public bool ShouldWeUseVirtualKeyboardDelegate() => AndroidUsesVKDelegate;
#elif !UNITY_EDITOR && UNITY_IOS
	    public bool ShouldWeUseVirtualKeyboardDelegate() => IOSUsesVKDelegate;
#endif
    }
}
