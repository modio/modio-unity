using ModIOBrowser.Implementation;
using UnityEngine;

namespace ModIOBrowser
{
    [CreateAssetMenu(fileName = "UiSettings.asset", menuName = "ModIo/UiSettings")]
    public partial class UiSettings : ScriptableObject
    {
	    public bool StandaloneUsesVKDelegate = true;
	    
        public TranslatedLanguages Language = TranslatedLanguages.English;
        
        [HideInInspector]
        public GlyphPlatforms GlyphPlatform;

        [Range(0f, 1f)]
        public float volume = 1f;
        
#if UNITY_EDITOR || UNITY_STANDALONE
        public bool ShouldWeUseVirtualKeyboardDelegate()
        {
	        return StandaloneUsesVKDelegate;
        }
#endif
    }
}
