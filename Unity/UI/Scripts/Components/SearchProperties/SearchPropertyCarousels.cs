using System;
using System.Collections.Generic;
using Modio.Monetization;
using Modio.Search;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable, MovedFrom(false, null, null, "SearchPropertySubCarousels"),]
    public class SearchPropertyCarousels : ISearchProperty, IPropertyMonoBehaviourEvents
    {
        [Serializable]
        class CarouselDefinition
        {
            public GameObject GameObject;
            public CarouselStyle Style;

            public readonly Stack<ModioUISearch> Pool = new Stack<ModioUISearch>();
            public readonly Stack<ModioUISearch> Active = new Stack<ModioUISearch>();
        }

        [SerializeField] GameObject _carouselParent;
        [SerializeField]
        CarouselDefinition[] _carouselDefinitions;
        [SerializeField] 
        GameObject _disableWhenNoCarousels;
        IModioSearchSettings _searchLastSearchSettingsFrom;

        public void Start()
        {
            foreach (CarouselDefinition carouselDefinition in _carouselDefinitions)
            {
                carouselDefinition.GameObject.SetActive(false);
                carouselDefinition.Pool.Push(carouselDefinition.GameObject.GetComponent<ModioUISearch>());
            }
        }

        public void OnSearchUpdate(ModioUISearch search)
        {
            IModioSearchSettings searchSettings = search.LastSearchSettingsFrom;

            if (search.ModioSearch.HasCustomSearch()) searchSettings = null;
            if (search.ModioSearch.IsSearching) searchSettings = null;

            if(_searchLastSearchSettingsFrom == searchSettings )
                return;
            
            _searchLastSearchSettingsFrom = searchSettings;

            foreach (CarouselDefinition carouselDefinition in _carouselDefinitions)
            {
                while (carouselDefinition.Active.TryPop(out ModioUISearch active))
                {
                    active.gameObject.SetActive(false);
                    carouselDefinition.Pool.Push(active);
                }
            }

            var carousels = (searchSettings as IModioSearchCarouselProvider)?.Carousels;

            if (_carouselParent != null) _carouselParent.SetActive(carousels is { Count: > 0, });
            
            if (_disableWhenNoCarousels != null) 
                _disableWhenNoCarousels.SetActive(carousels is { Count: > 0, });

            if (carousels == null) return;

            bool showMonetizationUI = ModioClient.Settings.GetPlatformSettings<MonetizationSettings>() != null;
            
            int siblingIndex = 0;
            foreach (var carouselSettings in carousels)
            {
                if (!showMonetizationUI && carouselSettings.settings.HiddenIfMonetizationDisabled)
                {
                    continue;
                }
                
                ModioUISearch modioUISearch = GetCarousel(carouselSettings.style);

                if (modioUISearch != null)
                {
                    modioUISearch.transform.SetSiblingIndex(siblingIndex++);
                    modioUISearch.SetSearch(carouselSettings.settings, true);
                }
            }
        }

        ModioUISearch GetCarousel(CarouselStyle style)
        {
            foreach (CarouselDefinition carouselDefinition in _carouselDefinitions)
            {
                if (carouselDefinition.Style != style)
                    continue;
                
                if (carouselDefinition.Pool.TryPop(out ModioUISearch fromPool))
                {
                    fromPool.gameObject.SetActive(true);
                    carouselDefinition.Active.Push(fromPool);
                    
                    return fromPool;
                }
                
                var modioUISearchPrefab = carouselDefinition.GameObject.GetComponent<ModioUISearch>();

                ModioUISearch created = Object.Instantiate(modioUISearchPrefab, modioUISearchPrefab.transform.parent);
                    
                created.gameObject.SetActive(true);
                carouselDefinition.Active.Push(created);
                return created;
            }

            return null;
        }

        public void OnDestroy()
        {
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }
    }
}
