using System;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Components.Selectables.Transitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes.Options
{
    [Serializable]
    public class SimpleColorOption : IStyleComponent<TMP_Text>, IStyleComponent<Image>, IStyleComponent<ModioUISelectableTransitions>
    {
        public ThemeOptions OptionType => _option;

        [SerializeField] Color _color;
        [SerializeField] ThemeOptions _option;

        public void TryStyleComponent(Object component)
        {
            if (component is Image image) StyleComponent(image);
            else if (component is TMP_Text text) StyleComponent(text);
            else if (component is ModioUISelectableTransitions transitions) StyleComponent(transitions);
        }

        public void StyleComponent(ModioUISelectableTransitions transitions)
        {
            if (transitions.FilteredToggle == ModioUISelectableTransitions.ToggleFilter.OnlyOn) 
                return;

            foreach (ISelectableTransition transition in transitions.SelectableTransitions)
            {
                if (transition is not SelectableTransitionColorTint tint) continue;

                var block = ColorBlock.defaultColorBlock;
                block.normalColor = _color;
                block.disabledColor = _color / 2;
                block.highlightedColor = _color;
                block.pressedColor = _color;
                block.selectedColor = _color;

                tint.ColorBlock = block;
            }
            transitions.RefreshCurrentState();
        }

        public void StyleComponent(TMP_Text text)
        {
            text.color = _color;
        }

        public void StyleComponent(Image image)
        {
            image.color = _color;
        }
    }

    [Serializable]
    public class CommonThemePropertyReference<T>
    {
        public string Name;
        public T ExplicitValue;
        
        public T ResolveFromTheme() => string.IsNullOrEmpty(Name) ? ExplicitValue : ModioUIThemeSheet.ResolveReferencedProperty<T>(Name);
    }
    
    [Serializable]
    public class SimpleColorOptionV2 : IStyleComponent<TMP_Text>, IStyleComponent<Image>, IStyleComponent<ModioUISelectableTransitions>
    {
        public ThemeOptions OptionType => _option;

        [SerializeField] ThemeOptions _option;
        [SerializeField] CommonThemePropertyReference<Color> _color2;

        public void TryStyleComponent(Object component)
        {
            if (component is Image image) StyleComponent(image);
            else if (component is TMP_Text text) StyleComponent(text);
            else if (component is ModioUISelectableTransitions transitions) StyleComponent(transitions);
        }

        public void StyleComponent(TMP_Text text)
        {
            Color resolveFromTheme = _color2.ResolveFromTheme();
            text.color = resolveFromTheme;
        }

        public void StyleComponent(Image image)
        {
            image.color = _color2.ResolveFromTheme();
        }

        public void StyleComponent(ModioUISelectableTransitions transitions)
        {
            foreach (ISelectableTransition transition in transitions.SelectableTransitions)
            {
                if (transition is not SelectableTransitionColorTint tint) continue;

                var block = ColorBlock.defaultColorBlock;
                Color color = _color2.ResolveFromTheme();
                block.normalColor = color;
                block.disabledColor = color / 2;
                block.highlightedColor = color;
                block.pressedColor = color;
                block.selectedColor = color;

                tint.ColorBlock = block;
            }

            transitions.RefreshCurrentState();
        }
    }
}
