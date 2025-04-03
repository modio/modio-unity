using System;
using Modio.API;
using UnityEngine;

namespace Modio.Unity
{
    [CreateAssetMenu(fileName = "config.asset", menuName = "ModIo/v3/config")]
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
    }
}
