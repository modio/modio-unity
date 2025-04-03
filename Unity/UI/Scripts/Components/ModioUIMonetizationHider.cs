using Modio.API;
using Modio.Unity.Settings;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUIMonetizationHider : MonoBehaviour
    {
        bool _isOffline;
        bool _isMonetizationDisabled;
        
        void Start()
        {
            ModioClient.OnInitialized += OnPluginInitialized;
            ModioAPI.OnOfflineStatusChanged += OnOfflineStatusChanged;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= OnPluginInitialized;
            ModioAPI.OnOfflineStatusChanged -= OnOfflineStatusChanged;
        }

        void OnOfflineStatusChanged(bool isOffline)
        {
            _isOffline = isOffline;
            
            ChangeActiveStateIfNeeded();
        }

        void OnPluginInitialized()
        {
            var settings = ModioServices.Resolve<ModioSettings>();

            _isMonetizationDisabled =
                settings.GetPlatformSettings<ModioComponentUISettings>() is not { ShowMonetizationUI: true, };
            
            ChangeActiveStateIfNeeded();
        }

        void ChangeActiveStateIfNeeded() => gameObject.SetActive(!(_isOffline || _isMonetizationDisabled));
    }
}
