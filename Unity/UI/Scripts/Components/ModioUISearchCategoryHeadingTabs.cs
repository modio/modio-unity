using System.Collections.Generic;
using Modio.Unity.UI.Search;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    /// <summary>
    /// Overrides the default tabs in the header with new options, either from
    /// the "Default Searches" array you can configure on this object in the inspector,
    /// or from a ModioUISearchCategoryTabOverrideSettings on your config if one is present
    /// </summary>
    public class ModioUISearchCategoryHeadingTabs : MonoBehaviour
    {
        [SerializeField] List<ModioUISearchCategoryTab> _tabs;
        [SerializeField] ModioUISearchSettings[] _defaultSearches;

        void Start()
        {
            ModioClient.OnInitialized += OnInitialized;
        }
        void OnDestroy()
        {
            ModioClient.OnInitialized -= OnInitialized;
        }

        void OnInitialized()
        {
            ModioUISearchSettings[] searches = _defaultSearches;

            if (ModioServices.TryResolve(out ModioSettings settings) &&
                settings.TryGetPlatformSettings(out ModioUISearchCategoryTabOverrideSettings overrideSettings))
            {
                searches = overrideSettings.OverrideMainSearchesTo;
            }
            
            for (int i = 0; i < searches.Length; i++)
            {
                ModioUISearchCategoryTab tab;

                if (i < _tabs.Count)
                {
                    tab = _tabs[i];
                }
                else
                {
                    ModioUISearchCategoryTab prev = _tabs[^1];
                    tab = Instantiate(prev, prev.transform.parent);
                    tab.transform.SetSiblingIndex(prev.transform.GetSiblingIndex()+1);
                    _tabs.Add(tab);
                }
                
                tab.SetSearchAndIndex(searches[i], i);
                tab.gameObject.SetActive(true);
            }
            
            for(int i = searches.Length; i < _tabs.Count; i++)
                _tabs[i].gameObject.SetActive(false);
        }
    }
}
