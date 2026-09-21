using System;
using System.Collections.Generic;
using System.Linq;
using Modio.Mods;
using Modio.Monetization;
using Modio.Search;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertySortTypeButtons : ISearchProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] Toggle _togglePrefab;
        [SerializeField] ToggleGroup _toggleGroup;
        
        readonly List<(Toggle toggle, SortModsBy sortBy)> _toggles = new();

        bool _isCollections;
        
        public void OnSearchUpdate(ModioUISearch search)
        {
            SortModsBy currentSortBy = search.ModioSearch.LastSearchFilter.SortBy;

            foreach ((Toggle toggle, SortModsBy sortBy) in _toggles) 
                toggle.isOn = sortBy == currentSortBy;

            bool searchingCollections = search.ModioSearch.LastSearchPreset is SpecialSearchType.SearchCollections or SpecialSearchType.FollowedCollections;

            if (_isCollections != searchingCollections)
            {
                _isCollections = searchingCollections;
                UpdateEnabledToggles();
            }
        }

        public void Start()
        {
            ModioClient.OnInitialized += ModioClientOnOnInitialized;
            
            var toggle = _toggleGroup.GetComponent<ModioUIToggle>();
            if (toggle != null)
                toggle.onValueChanged.AddListener(OnCategoryToggled);
        }

        public void OnDestroy() 
        {
            var toggle = _toggleGroup.GetComponent<ModioUIToggle>();
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnCategoryToggled);
        }

        void OnCategoryToggled(bool expanded)
        {
            foreach ((Toggle toggle, SortModsBy sortBy) toggle in _toggles)
                toggle.toggle.gameObject.SetActive(expanded);
        }

        void ModioClientOnOnInitialized()
        {
            if (_toggles.Count == 0)
            {
                var sortOptions = new []{
                    SortModsBy.DateSubmitted,
                    SortModsBy.DateUpdated,
                    //Trending?
                    SortModsBy.Popular,
                    SortModsBy.Downloads,
                    SortModsBy.Subscribers,
                    SortModsBy.Rating,
                    SortModsBy.Name,
                    SortModsBy.Price,
                };

                foreach (SortModsBy sortOption in sortOptions)
                {
                    Toggle toggle;

                    if (_toggles.Count == 0)
                        toggle = _togglePrefab;
                    else
                    {
                        toggle = Object.Instantiate(_togglePrefab, _togglePrefab.transform.parent, true);
                    }

                    _toggles.Add((toggle, sortOption));
                    toggle.group = _toggleGroup;
                    
                    var modioUILocalizedText = toggle.GetComponentInChildren<ModioUILocalizedText>();

                    if (modioUILocalizedText != null)
                    {
                        string localizationKey = SearchPropertySortType.GetLocalizationKey(sortOption);
                        modioUILocalizedText.SetKey(localizationKey);
                    }
                }
            }

            UpdateEnabledToggles();
        }

        void UpdateEnabledToggles()
        {
            foreach ((Toggle toggle, SortModsBy sortBy) in _toggles)
            {
                bool isValid = sortBy is not SortModsBy.Price || ModioClient.Settings.TryGetPlatformSettings(out MonetizationSettings _);
                isValid &= !_isCollections || sortBy is SortModsBy.DateSubmitted or SortModsBy.DateUpdated or SortModsBy.Popular or SortModsBy.Rating or SortModsBy.Name;  
                toggle.gameObject.SetActive(isValid);
            }
        }
        
        public void OnEnable() { }

        public void OnDisable()
        {
            ApplySort();
        }
        
        
        public void ApplySort()
        {
            var selectedToggle = _toggles.FirstOrDefault(toggle => toggle.toggle.isOn);

            if (selectedToggle.toggle == null) return;

            if (selectedToggle.sortBy == ModioUISearch.Default.ModioSearch.LastSearchFilter.SortBy)
            {
                return;
            }

            bool ascending = selectedToggle.sortBy switch
            {
                SortModsBy.Name    => true,
                SortModsBy.Price   => false,
                SortModsBy.Rating  => true,
                SortModsBy.Popular => false,
                SortModsBy.Downloads =>
                    true, // Note: this is a mistake on the backend api. Ascending is swapped with descending for this field
                SortModsBy.Subscribers   => true,
                SortModsBy.DateSubmitted => false,
                SortModsBy.DateUpdated => false,
                _                        => throw new ArgumentOutOfRangeException()
            };

            ModioUISearch.Default.ModioSearch.ApplySortBy(selectedToggle.sortBy, ascending);
        }
    }
}
