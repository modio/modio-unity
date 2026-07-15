using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Modio.API;
using Modio.Monetization;
using Modio.Platforms.Wss;
using UnityEngine;

namespace Modio.Unity
{
    [CreateAssetMenu(fileName = "config.asset", menuName = "Modio/v3/config")]
    public class ModioUnitySettings : ScriptableObject
    {
        public const string DefaultResourceName = "mod.io/v3_config";
        public const string DefaultResourceNameOverride = "mod.io/v3_config_local";

        [SerializeField]
        ModioSettings _settings;

        [SerializeField, SerializeReference] 
        IModioServiceSettings[] _platformSettings;

        public ModioSettings Settings
        {
            get
            {
                _settings.PlatformSettings = _platformSettings;
                return _settings;
            }
        }

        public void InvokeOnChanged() => Settings.InvokeOnChanged();

        internal void SetPlatformSettings(IModioServiceSettings[] newSettings)
        {
            _platformSettings = newSettings;
            InvokeOnChanged();
        }
        
        [ExcludeFromCodeCoverage]
        public static ModioUnitySettings LoadSettings()
        {
            ModioUnitySettings foundSetting = null;

            if (ModioCommandLine.TryGetArgument("unity-settings", out string target))
            {
                foundSetting = Resources.Load<ModioUnitySettings>($"mod.io/{target}");
                if (foundSetting == null)
                    foundSetting = Resources.Load<ModioUnitySettings>($"mod.io/v3_config_{target}");
            }

            if(foundSetting == null)
                foundSetting = Resources.Load<ModioUnitySettings>(ModioUnitySettings.DefaultResourceNameOverride);
            if (foundSetting == null)
                foundSetting = Resources.Load<ModioUnitySettings>(ModioUnitySettings.DefaultResourceName);

            if (ModioCommandLine.TryGetArgument("gameid", out string gameId))
                foundSetting.Settings.GameId = int.Parse(gameId);

            if (ModioCommandLine.TryGetArgument("apikey", out string apiKey))
                foundSetting.Settings.APIKey = apiKey;

            if (ModioCommandLine.TryGetArgument("url", out string url))
                foundSetting.Settings.ServerURL = url;
            
            if (ModioCommandLine.HasFlag("use-wss"))
                if(!foundSetting.Settings.TryGetPlatformSettings(out WssSettings _))
                {
                    var wssSettings = new WssSettings();
                    foundSetting.Settings.PlatformSettings = foundSetting.Settings.PlatformSettings.Append(wssSettings).ToArray();
                }
            
            if (ModioCommandLine.TryGetArgument("monetizationtype", out string monetizationType))
            {
                if(foundSetting.Settings.TryGetPlatformSettings(out MonetizationSettings monetizationSettings))
                    monetizationSettings.MonetizationType = Enum.Parse<ModioMonetizationType>(monetizationType, true);
                else
                {
                    monetizationSettings = new MonetizationSettings { MonetizationType = Enum.Parse<ModioMonetizationType>(monetizationType, true), };
                    foundSetting.Settings.PlatformSettings = foundSetting.Settings.PlatformSettings.Append(monetizationSettings).ToArray();
                    
                }
            }
            
            // If command line arg present we need to mutate the config
            if (ModioCommandLine.TryGetArgument("log", out string logLevelText)
                || ModioCommandLine.TryGetArgument("loglevel", out logLevelText))
            {
                if (Enum.TryParse(logLevelText, true, out LogLevel logLevelEnum))
                {
                    foundSetting.Settings.LogLevel = logLevelEnum;
                }
                else
                    // ReSharper disable once ExpressionIsAlwaysNull (it's set in ApplyLogLevel)
                    // ReSharper disable once ConstantConditionalAccessQualifier
                    ModioLog.Error?.Log($"Unrecognized log level: {logLevelText}");
            }
            
            return foundSetting;
        }
    }
}
