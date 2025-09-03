using System;
using UnityEngine;

namespace Modio.Unity.UI.Scripts.Themes
{
    public class ModioThemeController : MonoBehaviour
    {
        /// <summary>
        /// This event will fire whenever a value in the theme sheet has been updated or if the theme sheet itself has
        /// been changed. Use this then reference <see cref="Theme"/> to apply styling to components.
        /// </summary>
        public static event Action OnThemeSheetUpdated
        {
            add
            {
                if (_instance is not null && _instance._postedStyleEvent)
                    value.Invoke();

                _onThemeSheetUpdatedInternal += value;
            }
            remove => _onThemeSheetUpdatedInternal -= value;
        }

        static event Action _onThemeSheetUpdatedInternal;
        
        /// <summary>
        /// The current theme sheet containing all the mod.io styling options. This will only be available in play mode
        /// and after the Awake step.
        /// </summary>
        public static ModioUIThemeSheet Theme => _instance != null ? _instance._themeSheet : null;

        static ModioThemeController _instance;

        bool _postedStyleEvent;
        
        ModioUIThemeSheet _themeSheet;
        
        void Awake()
        {
            if (_instance is not null) return;

            _instance = this;
            
            if (ModioServices.TryResolve(out ModioSettings settings)) 
                OnModioSettingsUpdated(settings);
            
            ModioSettings.OnSettingsUpdated += OnModioSettingsUpdated;
        }
        
        void OnDestroy()
        {
            ModioSettings.OnSettingsUpdated -= OnModioSettingsUpdated;

            if (_themeSheet is not null)
            {
                _themeSheet.OnThemeSheetUpdated -= InvokeThemeSheetUpdated;
                _themeSheet = null;    
            }
            
            _instance = null;
        }

        void OnModioSettingsUpdated(ModioSettings settings)
        {
            if (!settings.TryGetPlatformSettings(out ModioThemeSystemSettings themeSettings)
                || !Application.isPlaying
                || _themeSheet == themeSettings.ThemeSheet) 
                return;
            
            if (_themeSheet is not null)
                _themeSheet.OnThemeSheetUpdated -= InvokeThemeSheetUpdated;

            _themeSheet = themeSettings.ThemeSheet;
            InvokeThemeSheetUpdated();
            _themeSheet.OnThemeSheetUpdated += InvokeThemeSheetUpdated;
        }

        void InvokeThemeSheetUpdated()
        {
            _onThemeSheetUpdatedInternal?.Invoke();
            if (!_postedStyleEvent) _postedStyleEvent = true;
        }

        void OnValidate()
        {
            if (_instance is null || _themeSheet is null) return;
            
            InvokeThemeSheetUpdated();
        }

        public static void SetThemeSheet(ModioUIThemeSheet sheet)
        {
            if (_instance is null)
            {
                ModioLog.Error?.Log($"No {nameof(ModioThemeController)} instance in scene! Cannot set theme!");
                return;
            }

            _instance.SetThemeSheetInternal(sheet);
        }

        void SetThemeSheetInternal(ModioUIThemeSheet sheet)
        {
            if (_themeSheet is not null) 
                _themeSheet.OnThemeSheetUpdated -= InvokeThemeSheetUpdated;
            
            _themeSheet = sheet;

            _themeSheet.OnThemeSheetUpdated += InvokeThemeSheetUpdated;
            
            _instance.InvokeThemeSheetUpdated();
        }
    }
}
