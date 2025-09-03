using System.Linq;
using Modio.API;
using Modio.Authentication;
using Modio.Extensions;
using Modio.FileIO;
using Modio.Unity.Settings;
using Modio.Unity.UI.Scripts.Themes;
using UnityEngine;

namespace Modio.Unity.UI.Panels
{
    public class ModioExampleSettingsPanel : ModioPanelBase
    {
        bool _hasDoneSetup;
        ModioSettings _settings = new ModioSettings();
        ModioDebugMenu _debugMenu;
        ModioUIThemeSheet _currentlySelectedSheet = null;

        void OnEnable()
        {
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

        public override void OnGainedFocus(GainedFocusCause selectionBehaviour)
        {
            base.OnGainedFocus(selectionBehaviour);
            if(selectionBehaviour == GainedFocusCause.OpeningFromClosed)
                SetupButtons();
        }

        void SetupButtons()
        {
            if(_hasDoneSetup) return;
            _hasDoneSetup = true;

            _debugMenu = GetComponent<ModioDebugMenu>();

            _debugMenu.AddLabel("Enter the Game Id and Key for the game you'd like to browse mods for.\n" +
                                "Then, scroll down and hit 'Apply changed settings'");
            _debugMenu.AddTextField("Game Id:", () => _settings.GameId, id =>
            {
                _settings.GameId = id;

                if (_settings.ServerURL.Contains("api-staging"))
                    _settings.ServerURL = StagingUrl();
                else if (_settings.ServerURL.Contains("test"))
                    _settings.ServerURL = TestUrl(_settings.GameId);
                else
                    _settings.ServerURL = ProductionUrl(_settings.GameId);
            });
            _debugMenu.AddTextField("Game Key:", () => _settings.APIKey, key => _settings.APIKey = key);

            _debugMenu.AddToggle(
                "Production Environment",
                () => !_settings.ServerURL.Contains("api-staging") && !_settings.ServerURL.Contains("test"),
                production =>
                {
                    if (production) _settings.ServerURL = ProductionUrl(_settings.GameId);
                    _debugMenu.SetToDefaults();
                }
            );

            _debugMenu.AddToggle(
                "Staging Environment",
                () => _settings.ServerURL.Contains("api-staging"),
                staging =>
                {
                    if (staging) _settings.ServerURL = StagingUrl();
                    _debugMenu.SetToDefaults();
                }
            );

            _debugMenu.AddToggle(
                "Test Environment",
                () => _settings.ServerURL.Contains("test"),
                test =>
                {
                    if (test) _settings.ServerURL = TestUrl(_settings.GameId);
                    _debugMenu.SetToDefaults();
                }
            );
            
            
            _debugMenu.AddTextField("Default Language:", () => _settings.DefaultLanguage, isoCode => _settings.DefaultLanguage = isoCode);

            _debugMenu.AddLabel("\nTUI Settings");
            _debugMenu.AddToggle("Show monetization", 
                    () => Get<ModioComponentUISettings>().ShowMonetizationUI,
                    on => Get<ModioComponentUISettings>().ShowMonetizationUI = on);
            _debugMenu.AddToggle("Show enabled", 
                    () => Get<ModioComponentUISettings>().ShowEnableModToggle,
                    on => Get<ModioComponentUISettings>().ShowEnableModToggle = on);
            _debugMenu.AddToggle("Fallback to email authentication", 
                    () => Get<ModioComponentUISettings>().FallbackToEmailAuthentication,
                    on => Get<ModioComponentUISettings>().FallbackToEmailAuthentication = on);
            
            _debugMenu.AddLabel("\nDisk Settings");
            _debugMenu.AddToggle("Override Disk Space Remaining", 
                                 () => Get<ModioDiskTestSettings>().OverrideDiskSpaceRemaining,
                                 on => Get<ModioDiskTestSettings>().OverrideDiskSpaceRemaining = on);
            _debugMenu.AddTextField("Fake Bytes Remaining", 
                                 () => Get<ModioDiskTestSettings>().BytesRemaining,
                                 on => Get<ModioDiskTestSettings>().BytesRemaining = on);
            
            _debugMenu.AddLabel("\nNetwork Settings");
            _debugMenu.AddToggle("Fake Disconnected (global)", 
                                 () => Get<ModioAPITestSettings>().FakeDisconnected,
                                 on => Get<ModioAPITestSettings>().FakeDisconnected = on);
            _debugMenu.AddTextField("Fake Disconnected (regex)",
                                    () => Get<ModioAPITestSettings>().FakeDisconnectedOnEndpointRegex,
                                    regex => Get<ModioAPITestSettings>().FakeDisconnectedOnEndpointRegex = regex);
            _debugMenu.AddToggle("Fake Ratelimit (global)", 
                                 () => Get<ModioAPITestSettings>().RateLimitError,
                                 on => Get<ModioAPITestSettings>().RateLimitError = on);
            _debugMenu.AddTextField("Fake Ratelimit (regex)",
                                    () => Get<ModioAPITestSettings>().RateLimitOnEndpointRegex,
                                    regex => Get<ModioAPITestSettings>().RateLimitOnEndpointRegex = regex);
            
            
            _debugMenu.AddLabel("\nIn browser debug menu");
            _debugMenu.AddToggle("Enable", 
                                 () => _settings.TryGetPlatformSettings(out ModioEnableDebugMenu _),
                                 on =>
                                 {
                                     if (on) Get<ModioEnableDebugMenu>();
                                     else
                                         _settings.PlatformSettings = _settings.PlatformSettings
                                                                               .Where(s => s is not ModioEnableDebugMenu)
                                                                               .ToArray();
                                 });
            
            //spacer
            _debugMenu.AddLabel("");

            _debugMenu.AddButton("Apply Changed Settings",
                                () =>
                                {
                                    ModioServices.BindInstance(_settings, ModioServicePriority.PlatformProvided + 10);
                                    ModioClient.Shutdown().ForgetTaskSafely();
                                    ClosePanel();
                                });
            _debugMenu.AddButton("Cancel Changed Settings",
                                () =>
                                {
                                    if (ModioServices.TryResolve(out ModioSettings settings)) 
                                        _settings = settings.ShallowClone();
                                    _debugMenu.SetToDefaults();
                                });
            
            _debugMenu.AddLabel("\nAuth Platform");
            ModioMultiplatformAuthResolver.Initialize();
            foreach (IModioAuthService modioAuthPlatform in ModioMultiplatformAuthResolver.AuthBindings)
            {
                _debugMenu.AddToggle(ModioDebugMenu.Nicify(modioAuthPlatform.GetType().Name), 
                                     () => ModioMultiplatformAuthResolver.ServiceOverride == modioAuthPlatform,
                                     on =>
                                     {
                                         if (on) ModioMultiplatformAuthResolver.ServiceOverride = modioAuthPlatform;
                                         _debugMenu.SetToDefaults();
                                         if(ModioClient.IsInitialized)
                                             ModioClient.Shutdown().ForgetTaskSafely();
                                     }
                );
            }

            var sheets = Resources.LoadAll<ModioUIThemeSheet>("mod.io/ThemeSheets");

            if (sheets.Length > 0)
            {
                
                _debugMenu.AddLabel("\nTheme Sheets");

                foreach (ModioUIThemeSheet sheet in sheets)
                {
                    _debugMenu.AddToggle(
                        ModioDebugMenu.Nicify(sheet.name),
                        () => ModioThemeController.Theme is not null &&
                              ReferenceEquals(ModioThemeController.Theme, sheet),
                        on =>
                        {
                            if (on) ModioThemeController.SetThemeSheet(sheet);
                            _debugMenu.SetToDefaults();
                        }
                    );
                }
            }

            _debugMenu.AddLabel("\nMisc Discovered Settings");
            _debugMenu.AddAllMethodsOrPropertiesWithAttribute<ModioDebugMenuAttribute>(attribute => attribute.ShowInSettingsMenu);
            
            _debugMenu.SetToDefaults();

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
            
            _debugMenu.AddButton("Close without applying some settings", ClosePanel);
        }

        string StagingUrl() => "https://api-staging.moddemo.io/v1";
        string ProductionUrl(long gameId) => $"https://g-{gameId}.modapi.io/v1";
        string TestUrl(long gameId) => $"https://g-{gameId}.test.mod.io/v1";
    }
}
