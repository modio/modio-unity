using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes
{
    public class ModioUIThemeProperties : MonoBehaviour
    {
        [SerializeField] StyleTarget _styleTarget;

        [SerializeField] StyledComponent[] _targetComponents = Array.Empty<StyledComponent>();
        
        void Start() => ModioThemeController.OnThemeSheetUpdated += ApplyStyling;

        void OnDestroy() => ModioThemeController.OnThemeSheetUpdated -= ApplyStyling;

        void ApplyStyling() => ApplyStyles(ModioThemeController.Theme);

        void ApplyStyles(ModioUIThemeSheet themeSheet)
        {
            foreach (StyledComponent property in _targetComponents) 
                themeSheet.ApplyStyle(_styleTarget, property.Option, property.Component);
        }
    }

    [Serializable]
    internal class StyledComponent
    {
        public Object Component;
        public ThemeOptions Option;
    }
}
