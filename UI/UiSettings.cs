using ModIOBrowser.Implementation;
using UnityEngine;

namespace ModIOBrowser
{
    [CreateAssetMenu(fileName = "UiSettings.asset", menuName = "ModIo/UiSettings")]
    public partial class UiSettings : ScriptableObject
    {
	    public bool StandaloneUsesVKDelegate = true;
	    
        public TranslatedLanguages Language;
        public GlyphPlatforms GlyphPlatform;
        
#if UNITY_EDITOR || UNITY_STANDALONE
        public bool ShouldWeUseVirtualKeyboardDelegate()
        {
	        return StandaloneUsesVKDelegate;
        }
#endif
    }
}
