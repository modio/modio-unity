using System;
using Modio.Collections;
using Modio.Errors;
using Modio.Mods;
using Modio.Search;
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
        [SerializeField] ModioUIModGroup _modGroupLibrary;
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
            if (!search.ModioSearch.IsSearching)
            {
                if (_resetScrollRect != null && !search.ModioSearch.IsAdditiveSearch)
                {
                    if(_resetScrollRect.vertical)
                        _resetScrollRect.verticalNormalizedPosition = 1;
                    if(_resetScrollRect.horizontal)
                        _resetScrollRect.horizontalNormalizedPosition = 0;
                }
                
                // Clear selections first if they're empty; this allows mods to steal selection back
                if((search.ModioSearch.LastSearchResultModCollections == null ||
                    search.ModioSearch.LastSearchResultModCollections.Count == 0) && _collectionGroup != null)
                    _collectionGroup.SetMods(Array.Empty<ModCollection>());
                if (_modGroup != null)
                {
                    bool isLibrarySearch = _modGroupLibrary != null &&
                                           search.ModioSearch.LastSearchPreset is SpecialSearchType.Installed
                                                                                  or SpecialSearchType
                                                                                      .InstalledOrSubscribed
                                                                                  or SpecialSearchType.Subscribed
                                                                                  or SpecialSearchType.Purchased
                                                                                  or SpecialSearchType.UserCreations;
                    
                    if(isLibrarySearch)
                        _modGroupLibrary.SetMods(search.ModioSearch.LastSearchResultMods, search.ModioSearch.LastSearchSelectionIndex); 
                    else
                        _modGroup.SetMods(search.ModioSearch.LastSearchResultMods, search.ModioSearch.LastSearchSelectionIndex);

                    _modGroup.gameObject.SetActive(search.ModioSearch.LastSearchResultMods.Count > 0 && !isLibrarySearch);
                    if(_modGroupLibrary != null)
                        _modGroupLibrary.gameObject.SetActive(search.ModioSearch.LastSearchResultMods.Count > 0 && isLibrarySearch);
                }
                if (_collectionGroup != null)
                {
                    _collectionGroup.SetMods(search.ModioSearch.LastSearchResultModCollections, search.ModioSearch.LastSearchSelectionIndex);
                    
                    _collectionGroup.gameObject.SetActive(search.ModioSearch.LastSearchResultModCollections?.Count > 0);
                }
                
                var handledNetworkError 
                    = _displayWhenOffline != null 
                      && search.ModioSearch.LastSearchError.Code == ErrorCode.CANNOT_OPEN_CONNECTION;

                if (search.ModioSearch.LastSearchError && !handledNetworkError)
                {
                    _errorHandler.Invoke(search.ModioSearch.LastSearchError);
                }

                if (_displayWhenOffline != null)
                    _displayWhenOffline.SetActive(handledNetworkError && search.ModioSearch.LastSearchResultMods.Count == 0 && search.ModioSearch.LastSearchResultModCollections.Count == 0);

                if (_displayWhenNoResults != null)
                    _displayWhenNoResults.SetActive(!handledNetworkError && search.ModioSearch.LastSearchResultMods.Count == 0 && search.ModioSearch.LastSearchResultModCollections.Count == 0
                                                    && search.ModioSearch.LastSearchPreset != SpecialSearchType.SubSearchesOnly && search.ModioSearch.LastSearchPreset != SpecialSearchType.VcPacks);
            }
            else
            {
                if (_displayWhenOffline != null) _displayWhenOffline.SetActive(false);

                if (_displayWhenNoResults != null) _displayWhenNoResults.SetActive(false);
            }
        }
    }
}
