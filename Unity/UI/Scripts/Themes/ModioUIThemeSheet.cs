using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Scripts.Themes.Options;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Scripts.Themes
{
    [CreateAssetMenu(fileName = "ModioUIThemeSheet", menuName = "Modio/UI/ThemeSheet", order = 1)]
    public class ModioUIThemeSheet : ScriptableObject
    {
        [Serializable]
        public class CommonThemeProperty<T>
        {
            public string Name;
            public CommonThemePropertyReference<T> ResolvesAs;
        }
        
        public event Action OnThemeSheetUpdated;

        [SerializeField] ModioUIThemeSheet _baseTheme;
        
        [SerializeField] CommonThemeProperty<Color>[] _colors = Array.Empty<CommonThemeProperty<Color>>();
        [SerializeField] Style[] _styles = Array.Empty<Style>();
        
        Dictionary<StyleTarget, Style> _stylesToThemes;

        static readonly List<IStyleOption> StyleOptionCrawlCache = new();
        static ModioUIThemeSheet _currentlyResolving;
        
        readonly List<IDictionary> _referencePropertyLookups = new();
        readonly Dictionary<(StyleTarget target, ThemeOptions option), IStyleOption[]> _styleLookupCache = new();

        public void ApplyStyle(StyleTarget target, ThemeOptions option, Object component)
        {
            SetAsCurrentPropertyProvider();
            
            //TODO: circular reference protection
            for (var resolving = this; resolving != null; resolving = resolving._baseTheme)
            {
                if (resolving._stylesToThemes is null)
                {
                    resolving._stylesToThemes = new Dictionary<StyleTarget, Style>();

                    foreach (Style styleToSort in resolving._styles)
                    {
                        resolving._stylesToThemes[styleToSort.Target] = styleToSort;
                    }
                }
            }
            
            Style style = GetStyleWithFallbacks(target);
            
            if (style == null) return;
            
            if(_referencePropertyLookups.Count ==0)
            {
                var colorDictionary = new Dictionary<string, Color>();

                for (var resolving = this; resolving != null; resolving = resolving._baseTheme)
                {
                    foreach (CommonThemeProperty<Color> commonThemeProperty in resolving._colors)
                    {
                        if (colorDictionary.ContainsKey(commonThemeProperty.Name)) continue;

                        colorDictionary[commonThemeProperty.Name] = commonThemeProperty.ResolvesAs.ResolveFromTheme();
                    }
                }

                _referencePropertyLookups.Add(colorDictionary);
            }
            
            if(!_styleLookupCache.TryGetValue((target, option), out IStyleOption[] styleOptions))
            {
                StyleOptionCrawlCache.Clear();
                
                var iterations = 10;

                while (iterations-- > 0 && style != null)
                {
                    for (var resolving = this; resolving != null; resolving = resolving._baseTheme)
                        if (resolving._stylesToThemes.TryGetValue(style.Target, out Style themeStyle))
                        {
                            for (var i = themeStyle.StyleOptions.Count - 1; i >= 0; i--)
                            {
                                IStyleOption styleOption = themeStyle.StyleOptions[i];

                                if (styleOption.OptionType != option) continue;

                                bool redundant = false;

                                foreach (IStyleOption prevStyle in StyleOptionCrawlCache)
                                {
                                    if (StyleObscures(styleOption, prevStyle))
                                    {
                                        redundant = true;
                                        break;
                                    }
                                }

                                if (!redundant) StyleOptionCrawlCache.Add(styleOption);
                            }
                        }

                    if (style.Extends == StyleTarget.None || style.Target == StyleTarget.Default) break;

                    style = GetStyleWithFallbacks(style.Extends);
                }

                styleOptions = StyleOptionCrawlCache.ToArray();
                _styleLookupCache[(target, option)] = styleOptions;
            }

            Style.DebugCurrentStyle = target;

            // We want to crawl backwards as we've inserted the most important style at the beginning
            for (int i = styleOptions.Length - 1; i >= 0; i--)
            {
                styleOptions[i].TryStyleComponent(component);
            }
        }

        //Check if a styleOption is made completely redundant by a style that will be applied after it
        static bool StyleObscures(IStyleOption style, IStyleOption obscuredBy)
        {
            if (IsColorOption(style, out var filter1) && IsColorOption(obscuredBy, out var filter2))
            {
                //Make sure the second filter doesn't work on any bits not in the first
                return (filter1 & filter2) == filter2;
            }

            return style.GetType() == obscuredBy.GetType();

            static bool IsColorOption(IStyleOption style, out ModioUISelectableTransitions.ToggleFilter matchStates)
            {
                matchStates = ModioUISelectableTransitions.ToggleFilter.Any;

                switch (style)
                {
                    case SimpleColorOption or SimpleColorOptionV2:
                        return true;
                    case ColorSchemeOption cso:
                        matchStates = cso.MatchStates;
                        return true;
                    case ColorSchemeOptionSimplified csos:
                        matchStates = csos.ToggleFilter;
                        return true;
                    default:
                        return false;
                }
            }
        }


        Style GetStyleWithFallbacks(StyleTarget target)
        {
            for (int maxIterations = 100; target != StyleTarget.None && maxIterations-- > 0; target = GetFallbackStyle(target))
            {
                for (var resolving = this; resolving != null; resolving = resolving._baseTheme)
                    if (resolving._stylesToThemes.TryGetValue(target, out Style style)) return style;
            }

            return null;
        }

        public void SetAsCurrentPropertyProvider()
        {
            _currentlyResolving = this;

            for (var resolving = _baseTheme; resolving != null; resolving = resolving._baseTheme)
            {
                resolving.OnThemeSheetUpdated -= BaseThemeUpdated;
                resolving.OnThemeSheetUpdated += BaseThemeUpdated;
            }
        }

        void BaseThemeUpdated()
        {
            ClearCache();
            OnThemeSheetUpdated?.Invoke();
        }

        static StyleTarget GetFallbackStyle(StyleTarget target)
        {
            return target switch
            {
                StyleTarget.None or StyleTarget.Default => StyleTarget.None,
                StyleTarget.Button                      => StyleTarget.Default,
                //Standard buttons
                StyleTarget.ButtonPrimaryAction
                    or StyleTarget.ButtonSpecial
                    or StyleTarget.ButtonRadio
                    or StyleTarget.ButtonPrivacyPolicy
                    or StyleTarget.ButtonWalletBalance
                    or StyleTarget.ButtonHamburger
                    or StyleTarget.ButtonModCreator
                    or StyleTarget.ButtonLibrarySort => StyleTarget.Button,
                //Non standard buttons
                StyleTarget.ButtonBrowserTab or StyleTarget.ButtonTag => StyleTarget.Default,
                StyleTarget.PanelModDisplayInfoBackground             => StyleTarget.PanelModDisplay,
                StyleTarget.ActionBindingGlyph                        => StyleTarget.None,
                StyleTarget.ModTileFeatured
                    or StyleTarget.ModTileFeaturedLarge
                    or StyleTarget.ModTilePremiumBranding
                    or StyleTarget.ModTileDisabledText
                    or StyleTarget.ModTileEnabledText =>
                    StyleTarget.ModTile,
                StyleTarget.ActionBindings
                    or StyleTarget.PanelBackground
                    or StyleTarget.PanelModDisplay
                    or StyleTarget.PanelModTileOptions
                    or StyleTarget.PanelFilter
                    or StyleTarget.BrowserHeader
                    or StyleTarget.FilterToggle
                    or StyleTarget.TextSubdued
                    or StyleTarget.InputField
                    or StyleTarget.ModStat
                    or StyleTarget.ModDisplayLibraryStatus
                    or StyleTarget.ModDisplayLibraryStatusWarning
                    or StyleTarget.ModTile
                    or StyleTarget.BrowserBackground => StyleTarget.Default,
                _ => StyleTarget.Default,
            };
        }

        void OnValidate()
        {
            ClearCache();
            OnThemeSheetUpdated?.Invoke();
        }

        void ClearCache()
        {
            _styleLookupCache.Clear();
            _referencePropertyLookups.Clear();
        }

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
        
        public static T ResolveReferencedProperty<T>(string propertyName)
        {
            if (_currentlyResolving == null)
            {
                ModioLog.Warning?.Log($"Theme couldn't resolve property {propertyName} as no themesheet is active");
                return default(T);
            }
            
            foreach (IDictionary dictionary in _currentlyResolving._referencePropertyLookups)
            {
                if (dictionary is not Dictionary<string, T> cache)
                    continue;

                if (cache.TryGetValue(propertyName, out T result)) 
                    return result;
                
                ModioLog.Warning?.Log($"Theme couldn't resolve property {propertyName} of type {typeof(T)}");
                return default(T);
            }
            
            for (var resolving = _currentlyResolving; resolving != null; resolving = resolving._baseTheme)
            {
                var resolvedArray = resolving._colors as CommonThemeProperty<T>[];

                if (resolvedArray == null)
                {
                    ModioLog.Error?.Log($"Can't resolve type {typeof(T)}. It probably hasn't been set up yet");
                    return default(T);
                }

                CommonThemeProperty<T> commonThemeProperty = resolvedArray.FirstOrDefault(c => string.Equals(
                                                                                              c.Name,
                                                                                              propertyName,
                                                                                              StringComparison.InvariantCultureIgnoreCase
                                                                                          )
                );

                if (commonThemeProperty == null)
                {
                    continue;
                }

                if (commonThemeProperty.ResolvesAs.Name == propertyName)
                {
                    Debug.LogError("Self reference detected in property " + propertyName);
                    return default(T);
                }

                T resolveReferencedProperty = commonThemeProperty.ResolvesAs.ResolveFromTheme();
                return resolveReferencedProperty;
            }
            
            ModioLog.Warning?.Log($"Theme couldn't resolve property {propertyName} of type {typeof(T)}");
            return default(T);
        }

        public static string[] GetAllReferencedPropertyNames<T>()
        {
            IEnumerable<string> names = null;

            for(var resolving = _currentlyResolving; resolving != null; resolving = resolving._baseTheme)
            {
                var resolvedArray = resolving._colors as CommonThemeProperty<T>[];

                if (resolvedArray == null)
                {
                    ModioLog.Error?.Log($"Can't resolve type {typeof(T)}. It probably hasn't been set up yet");
                    return Array.Empty<string>();
                }

                IEnumerable<string> enumerable = resolvedArray.Select(r => r.Name);
                names = names == null ? enumerable : names.Concat(enumerable).Distinct();
            }
            
            return names?.ToArray() ?? Array.Empty<string>();
        }
    }
    
    [Serializable]
    public class Style
    {
        public static StyleTarget DebugCurrentStyle;
        public static StyleTarget DebugCurrentStyleFrom;
        
        public StyleTarget Target => _target;
        public StyleTarget Extends => _extends;

        [SerializeField] StyleTarget _target;
        [SerializeField] StyleTarget _extends;
        [SerializeField, SerializeReference] IStyleOption[] _styleOptions = Array.Empty<IStyleOption>();

        public IReadOnlyList<IStyleOption> StyleOptions => _styleOptions;
        
        public void ApplyStyleToObject(Object component, ThemeOptions option)
        {
            DebugCurrentStyleFrom = _target;
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
