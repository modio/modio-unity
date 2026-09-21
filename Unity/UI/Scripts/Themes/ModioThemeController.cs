using System;
using System.Collections.Generic;
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

        public static void RegisterProperties(ModioUIThemeProperties properties)
        {
            if (_instance is not null && _instance._postedStyleEvent)
                properties.ApplyStyles(Theme);
            
            AllProperties.Add(properties);
        }
        
        public static void DeregisterProperties(ModioUIThemeProperties properties)
        {
            AllProperties.Remove(properties);
        }

        static event Action _onThemeSheetUpdatedInternal;
        static readonly HashSet<ModioUIThemeProperties> AllProperties = new();
        
        /// <summary>
        /// The current theme sheet containing all the mod.io styling options. This will only be available in play mode
        /// and after the Awake step.
        /// </summary>
        public static ModioUIThemeSheet Theme => _instance != null ? _instance._themeSheet : null;

        static ModioThemeController _instance;

        bool _postedStyleEvent;
        
        [SerializeField]
        ModioUIThemeSheet _defaultThemeSheet;
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
            if (!Application.isPlaying) 
                return;

            ModioUIThemeSheet themeSheet = _defaultThemeSheet;

            if (settings.TryGetPlatformSettings(out ModioThemeSystemSettings themeSettings)) themeSheet = themeSettings.ThemeSheet;

            if (_themeSheet == themeSheet) 
                return;

            if (_themeSheet != null)
                _themeSheet.OnThemeSheetUpdated -= InvokeThemeSheetUpdated;

            _themeSheet = themeSheet;
            InvokeThemeSheetUpdated();
            if (_themeSheet != null)
                _themeSheet.OnThemeSheetUpdated += InvokeThemeSheetUpdated;
        }

        void InvokeThemeSheetUpdated()
        {
            _onThemeSheetUpdatedInternal?.Invoke();
            
            foreach (ModioUIThemeProperties properties in AllProperties) 
                properties.ApplyStyles(Theme);

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
