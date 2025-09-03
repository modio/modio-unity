using System;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Components.Selectables.Transitions;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    [Serializable]
    public class ColorSchemeOption : BaseStyleOption<ModioUISelectableTransitions>
    {
        [SerializeField] bool _isOn;
        [SerializeField] ColorBlock _colorBlock;
        
        protected override void StyleComponent(ModioUISelectableTransitions component)
        {
            if ((component.FilteredToggle == ModioUISelectableTransitions.ToggleFilter.OnlyOff && _isOn)
                || (component.FilteredToggle == ModioUISelectableTransitions.ToggleFilter.OnlyOn && !_isOn)) 
                return;

            foreach (ISelectableTransition transition in component.SelectableTransitions)
            {
                if (transition is not SelectableTransitionColorTint tint) continue;

                ColorBlock test = _colorBlock;
                
                tint.ColorBlock = test;
            }
            
            component.RefreshCurrentState();
        }
    }
}
