using System.Linq;
using Modio.API;
using Modio.Unity.UI.Input;
using UnityEngine;

namespace Modio.Unity.UI.Panels
{
    /// <example>
    /// [ModioDebugMenu]
    /// static void ExampleButton()
    /// {
    ///     Debug.Log("Hello World");
    /// }
    /// </example>
    public class ModioDebugTestPanel : ModioPanelBase
    {
        bool _hasDoneHookup;
        ModioDebugMenu _modioDebugMenu;
        ModioSettings _settings;

        protected override void Awake()
        {
            _modioDebugMenu = GetComponent<ModioDebugMenu>();
            _modioDebugMenu.Awake();
        }

        void OnEnable()
        {
            if(ModioClient.Settings.TryGetPlatformSettings<ModioEnableDebugMenu>(out _))
                ModioUIInput.AddHandler(ModioUIInput.ModioAction.DeveloperMenu, OpenPanel);
            if (!ModioServices.TryResolve(out ModioSettings settings))
            {
                var unitySettings = Resources.Load<ModioUnitySettings>(ModioUnitySettings.DefaultResourceNameOverride);

                if (unitySettings == null) 
                    unitySettings = Resources.Load<ModioUnitySettings>(ModioUnitySettings.DefaultResourceName);

                if (unitySettings == null)
                {
                    Debug.LogError($"Couldn't find bound Settings or settings file");
                    return;
                }

                settings = unitySettings.Settings;
            }

            _settings = settings.ShallowClone();
        }

        void OnDisable()
        {
            ModioUIInput.RemoveHandler(ModioUIInput.ModioAction.DeveloperMenu, OpenPanel);
        }

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            FindAllHookups();
            
            base.OnGainedFocus(selectionBehaviour);
            
            if(selectionBehaviour == GainedFocusCause.OpeningFromClosed) _modioDebugMenu.SetToDefaults();
        }

        void FindAllHookups()
        {
            if (_hasDoneHookup) return;
            _hasDoneHookup = true;
            
            _modioDebugMenu.AddAllMethodsOrPropertiesWithAttribute<ModioDebugMenuAttribute>(attribute => attribute.ShowInBrowserMenu);
            
            _modioDebugMenu.AddLabel("\nNetwork Settings");
            _modioDebugMenu.AddToggle("Fake Disconnected (global)", 
                                      () => Get<ModioAPITestSettings>().FakeDisconnected,
                                      on => Get<ModioAPITestSettings>().FakeDisconnected = on);
            _modioDebugMenu.AddTextField("Fake Disconnected (regex)",
                                         () => Get<ModioAPITestSettings>().FakeDisconnectedOnEndpointRegex,
                                         regex => Get<ModioAPITestSettings>().FakeDisconnectedOnEndpointRegex = regex);
            _modioDebugMenu.AddToggle("Fake Ratelimit (global)", 
                                      () => Get<ModioAPITestSettings>().RateLimitError,
                                      on => Get<ModioAPITestSettings>().RateLimitError = on);
            _modioDebugMenu.AddTextField("Fake Ratelimit (regex)",
                                         () => Get<ModioAPITestSettings>().RateLimitOnEndpointRegex,
                                         regex => Get<ModioAPITestSettings>().RateLimitOnEndpointRegex = regex);
            
            //Note that this is required as _settings can change, whist the Toggle's and such aren't recreated
            T Get<T>() where T : IModioServiceSettings, new()
            {
                var platformSettings = _settings.GetPlatformSettings<T>();

                if (platformSettings == null)
                {
                    platformSettings = new T();
                    _settings.PlatformSettings = _settings.PlatformSettings.Append(platformSettings).ToArray();
                }

                return platformSettings;
            }
        }
    }
}
