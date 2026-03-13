using System;
using Modio.Collections;
using Modio.Errors;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyDisplayResults : ISearchProperty
    {
        [SerializeField] ModioUIModGroup _modGroup;
        [SerializeField] ModioUICollectionGroup _collectionGroup;

        [SerializeField, Tooltip("(Optional) Enable this gameObject when there are zero results")]
        GameObject _displayWhenNoResults;
        [SerializeField,
         Tooltip("(Optional) Enable this gameObject when there are network issues and there's no results")]
        GameObject _displayWhenOffline;

        [SerializeField] UnityEvent<Error> _errorHandler;
        
        [SerializeField] ScrollRect _resetScrollRect;

        public void OnSearchUpdate(ModioUISearch search)
        {
            if (!search.IsSearching)
            {
                if (_resetScrollRect != null && !search.IsAdditiveSearch)
                {
                    if(_resetScrollRect.vertical)
                        _resetScrollRect.verticalNormalizedPosition = 1;
                    if(_resetScrollRect.horizontal)
                        _resetScrollRect.horizontalNormalizedPosition = 0;
                }
                
                // Clear selections first if they're empty; this allows mods to steal selection back
                if(search.LastSearchResultModCollections == null || search.LastSearchResultModCollections.Count == 0 && _collectionGroup != null)
                    _collectionGroup.SetMods(Array.Empty<ModCollection>());
                if (_modGroup != null)
                {
                    _modGroup.SetMods(search.LastSearchResultMods, search.LastSearchSelectionIndex);

                    _modGroup.gameObject.SetActive(search.LastSearchResultMods.Count > 0);
                }
                if (_collectionGroup != null)
                {
                    _collectionGroup.SetMods(search.LastSearchResultModCollections, search.LastSearchSelectionIndex);
                    
                    _collectionGroup.gameObject.SetActive(search.LastSearchResultModCollections.Count > 0);
                }
                
                var handledNetworkError 
                    = _displayWhenOffline != null 
                      && search.LastSearchError.Code == ErrorCode.CANNOT_OPEN_CONNECTION;

                if (search.LastSearchError && !handledNetworkError)
                {
                    _errorHandler.Invoke(search.LastSearchError);
                }

                if (_displayWhenOffline != null)
                    _displayWhenOffline.SetActive(handledNetworkError && search.LastSearchResultMods.Count == 0 && search.LastSearchResultModCollections.Count == 0);

                if (_displayWhenNoResults != null)
                    _displayWhenNoResults.SetActive(!handledNetworkError && search.LastSearchResultMods.Count == 0 && search.LastSearchResultModCollections.Count == 0
                                                    && search.LastSearchPreset != SpecialSearchType.SubSearchesOnly);
            }
            else
            {
                if (_displayWhenOffline != null) _displayWhenOffline.SetActive(false);

                if (_displayWhenNoResults != null) _displayWhenNoResults.SetActive(false);
            }
        }
    }
}
