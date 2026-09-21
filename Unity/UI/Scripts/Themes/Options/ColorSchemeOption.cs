using System;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Components.Selectables.Transitions;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    [Serializable]
    public class ColorSchemeOption : IStyleComponent<TMP_Text>, IStyleComponent<Image>, IStyleComponent<ModioUISelectableTransitions>
    {
        public ThemeOptions OptionType => _option;

        public ModioUISelectableTransitions.ToggleFilter MatchStates => _isOn
            ? ModioUISelectableTransitions.ToggleFilter.OnlyOn
            : ModioUISelectableTransitions.ToggleFilter.OnlyOff;

        [SerializeField] ThemeOptions _option;
        
        [SerializeField] bool _isOn;
        [SerializeField] ColorBlock _colorBlock;
        
        public void TryStyleComponent(Object component)
        {
            if (component is ModioUISelectableTransitions selectableTransitions) StyleComponent(selectableTransitions);
            else if (component is Image image) StyleComponent(image);
            else if (component is TMP_Text text) StyleComponent(text);
        }

        public void StyleComponent(TMP_Text text)
        {
            text.color = _colorBlock.normalColor;
        }

        public void StyleComponent(Image image)
        {
            image.color = _colorBlock.normalColor;
        }

        public void StyleComponent(ModioUISelectableTransitions component)
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
    
    [Serializable]
    public class ColorSchemeOptionSimplified : IStyleComponent<TMP_Text>, IStyleComponent<Image>, IStyleComponent<ModioUISelectableTransitions>
    {
        public ThemeOptions OptionType => _option;

        [SerializeField] ThemeOptions _option;
        [SerializeField] ModioUISelectableTransitions.ToggleFilter _matchStates = ModioUISelectableTransitions.ToggleFilter.Any;
        
        [SerializeField] CommonThemePropertyReference<Color> _mainColor;
        [SerializeField] CommonThemePropertyReference<Color> _highlightColor;

        public ModioUISelectableTransitions.ToggleFilter ToggleFilter => _matchStates;
        
        public void TryStyleComponent(Object component)
        {
            if (component is ModioUISelectableTransitions selectableTransitions) StyleComponent(selectableTransitions);
            else if (component is Image image) StyleComponent(image);
            else if (component is TMP_Text text) StyleComponent(text);
        }

        public void StyleComponent(TMP_Text text)
        {
            text.color = _mainColor.ResolveFromTheme();
        }

        public void StyleComponent(Image image)
        {
            image.color = _mainColor.ResolveFromTheme();
        }

        public void StyleComponent(ModioUISelectableTransitions component)
        {
            if ((component.FilteredToggle & _matchStates) == 0)
                return;

            foreach (ISelectableTransition transition in component.SelectableTransitions)
            {
                if (transition is not SelectableTransitionColorTint tint) continue;

                var block = ColorBlock.defaultColorBlock;

                block.normalColor = _mainColor.ResolveFromTheme();
                block.disabledColor = block.normalColor / 2;
                block.highlightedColor = _highlightColor.ResolveFromTheme();
                block.pressedColor = block.highlightedColor;
                block.selectedColor = block.highlightedColor;
                
                tint.ColorBlock = block;
            }
            
            component.RefreshCurrentState();
        }
    }
}
