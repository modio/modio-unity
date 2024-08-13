#if UNITY_EDITOR
using System.Collections.Generic;
using ModIO.Implementation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ModIO.EditorCode
{
    public class ModioSettingProvider : SettingsProvider
    {
        SettingsAsset _config;
        SerializedObject _serializedConfig;

        ModioSettingProvider() :
            base("mod.io/Settings", SettingsScope.Project, new HashSet<string>(new[] { "modio", "gameId", "gameKey", "apiKey", "Server URL" }))
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _config = EditorMenu.GetConfigAsset();
            _serializedConfig = new SerializedObject(_config);

            rootElement.Add(new Label("mod.io Settings")
            {
                style =
                {
                    marginLeft = 4,
                    fontSize = 19,
                    unityFontStyleAndWeight = FontStyle.Bold,
                },
            });
            rootElement.Add(new InspectorElement(_serializedConfig));
        }

        [SettingsProvider]
        public static SettingsProvider OpenModioSettingsProvider() => new ModioSettingProvider();
    }
}
#endif
