using System.Collections.Generic;
using Modio.Unity;
using Modio.Unity.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Modio.Editor.Unity
{
    public class ModioSettingProvider : SettingsProvider
    {
        ModioUnitySettings _config;
        SerializedObject _serializedConfig;

        ModioSettingProvider() :
            base(
                "mod.io/Settings",
                SettingsScope.Project,
                new HashSet<string>(new[] { "modio", "gameId", "gameKey", "apiKey", "Server URL" })
            ) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _config = EditSettingsTool.GetSettingsAsset();
            _serializedConfig = new SerializedObject(_config);

            var mainContainer = new VisualElement { style = { paddingLeft = 4, }, };
            
            mainContainer.Add(
                new Label("mod.io Settings")
                {
                    style =
                    {
                        marginLeft = 4,
                        fontSize = 19,
                        unityFontStyleAndWeight = FontStyle.Bold,
                    },
                }
            );


            mainContainer.Add(new InspectorElement(_serializedConfig));
            
            rootElement.Add(mainContainer);
        }

        [SettingsProvider]
        public static SettingsProvider OpenModioSettingsProvider() => new ModioSettingProvider();
    }
}
