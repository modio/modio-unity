using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes
{
    public class ModioUIThemeProperties : MonoBehaviour
    {
        [SerializeField] StyleTarget _styleTarget;

        [SerializeField] StyledComponent[] _targetComponents = Array.Empty<StyledComponent>();
        
        void OnEnable() => ModioThemeController.RegisterProperties(this);

        void OnDisable() => ModioThemeController.DeregisterProperties(this);

        [ContextMenu("Apply Theme Immediately")]
        void ApplyStyling() => ApplyStyles(ModioThemeController.Theme);

        public void ApplyStyles(ModioUIThemeSheet themeSheet)
        {
            foreach (StyledComponent property in _targetComponents)
            {
                themeSheet.ApplyStyle(_styleTarget, property.Option, property.Component);
                if(property.Component is GameObject go)
                    Debug.LogError($"Suspicious theme property: Gameobject {go} when expecting a component", go);
            }
        }
    }

    [Serializable]
    internal class StyledComponent
    {
        public Object Component;
        public ThemeOptions Option;
    }
}
