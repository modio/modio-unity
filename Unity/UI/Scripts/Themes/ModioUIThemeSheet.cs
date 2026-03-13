using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes
{
    [CreateAssetMenu(fileName = "ModioUIThemeSheet", menuName = "Modio/UI/ThemeSheet", order = 1)]
    public class ModioUIThemeSheet : ScriptableObject
    {
        public event Action OnThemeSheetUpdated;
        
        [SerializeField] Style[] _styles = Array.Empty<Style>();
        
        Dictionary<StyleTarget, Style> _stylesToThemes;

        static readonly List<Style> StyleCrawlCache = new List<Style>();
        
        public void ApplyStyle(StyleTarget target, ThemeOptions option, Object component)
        {
            if (_stylesToThemes is null)
            {
                _stylesToThemes = new Dictionary<StyleTarget, Style>();

                foreach (Style styleToSort in _styles)
                {
                    _stylesToThemes[styleToSort.Target] = styleToSort;
                }
            }

            if (!_stylesToThemes.ContainsKey(StyleTarget.Default)) return;
            
            if (!_stylesToThemes.TryGetValue(target, out Style style))
            {
                style = _stylesToThemes[StyleTarget.Default];
            }
            
            StyleCrawlCache.Clear();
            
            StyleCrawlCache.Add(style);

            var iterations = 10;
            
            while (iterations > 0)
            {
                if (style.Extends == StyleTarget.None
                    || style.Target == StyleTarget.Default) break;

                if (_stylesToThemes.TryGetValue(style.Extends, out Style newStyle))
                {
                    StyleCrawlCache.Add(newStyle);
                    style = newStyle;
                }
                else
                    break;

                iterations--;
            }

            // We want to crawl backwards as we've inserted the most important style at the beginning
            for (int i = StyleCrawlCache.Count - 1; i >= 0; i--)
            {
                StyleCrawlCache[i].ApplyStyleToObject(component, option);
            }
        }

        void OnValidate() => OnThemeSheetUpdated?.Invoke();

        public void CompareAgainst(ModioUIThemeSheet compareTo)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Comparing {name} to {compareTo.name}");
            DoCompare(this, compareTo, "Added");
            DoCompare(compareTo, this, "Removed");
            
            Debug.Log(stringBuilder.ToString());
            return;

            void DoCompare(ModioUIThemeSheet a, ModioUIThemeSheet b, string message)
            {
                foreach (Style style in a._styles)
                {
                    bool matchedStyle = false;
                    foreach (Style compareStyle in b._styles)
                    {
                        if (compareStyle.Target != style.Target) continue;
                        matchedStyle = true;
                    }

                    if (!matchedStyle)
                    {
                        stringBuilder.AppendLine($"{message} {style.Target} (extending {style.Extends})");
                    }
                    
                    foreach (IStyleOption styleOption in style.StyleOptions)
                    {
                        bool foundMatch = false;
                        foreach (Style compareStyle in b._styles)
                        {
                            if (compareStyle.Target != style.Target) continue;

                            foreach (IStyleOption compareOption in compareStyle.StyleOptions)
                            {
                                if(compareOption.OptionType != styleOption.OptionType) continue;
                                if(compareOption.GetType() != styleOption.GetType()) continue;
                                foundMatch = true;
                                break;
                            }
                        
                            if(foundMatch) break;
                        }
                        if (!foundMatch)
                        {
                            stringBuilder.AppendLine(
                                $"{message} {style.Target}.{styleOption.OptionType} of type {styleOption.GetType().Name}"
                            );
                        }
                    }
                }
            }
        }
    }
    
    [Serializable]
    public class Style
    {
        public StyleTarget Target => _target;
        public StyleTarget Extends => _extends;

        [SerializeField] StyleTarget _target;
        [SerializeField] StyleTarget _extends;
        [SerializeField, SerializeReference] IStyleOption[] _styleOptions = Array.Empty<IStyleOption>();

        public IReadOnlyList<IStyleOption> StyleOptions => _styleOptions;
        
        public void ApplyStyleToObject(Object component, ThemeOptions option)
        {
            foreach (IStyleOption style in _styleOptions)
            {
                if (style.OptionType == option)
                {
                    style.TryStyleComponent(component);
                }
            }
        }
    }
}
