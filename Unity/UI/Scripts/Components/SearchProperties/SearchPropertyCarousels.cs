using System;
using System.Collections.Generic;
using Modio.Monetization;
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
            public ModioUISearchSettings.CarouselStyle Style;

            public readonly Stack<ModioUISearch> Pool = new Stack<ModioUISearch>();
            public readonly Stack<ModioUISearch> Active = new Stack<ModioUISearch>();
        }

        [SerializeField] GameObject _carouselParent;
        [SerializeField]
        CarouselDefinition[] _carouselDefinitions;
        [SerializeField] 
        GameObject _disableWhenNoCarousels;
        ModioUISearchSettings _searchLastSearchSettingsFrom;

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
            ModioUISearchSettings searchSettings = search.LastSearchSettingsFrom;

            if (search.HasCustomSearchOrFiltering()) searchSettings = null;
            
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
            
            if (_carouselParent != null) _carouselParent.SetActive(searchSettings?.Carousels is { Length: > 0, });
            
            if (_disableWhenNoCarousels != null) 
                _disableWhenNoCarousels.SetActive(searchSettings?.Carousels is { Length: > 0, });

            if (searchSettings?.Carousels == null) return;

            bool showMonetizationUI = ModioClient.Settings.GetPlatformSettings<MonetizationSettings>() != null;
            
            int siblingIndex = 0;
            foreach (var carouselSettings in searchSettings.Carousels)
            {
                if (!showMonetizationUI && carouselSettings.Search.HiddenIfMonetizationDisabled)
                {
                    continue;
                }
                
                ModioUISearch modioUISearch = GetCarousel(carouselSettings.Style);

                if (modioUISearch != null)
                {
                    modioUISearch.transform.SetSiblingIndex(siblingIndex++);
                    carouselSettings.Search.Search(modioUISearch);
                }
            }
        }

        ModioUISearch GetCarousel(ModioUISearchSettings.CarouselStyle style)
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
