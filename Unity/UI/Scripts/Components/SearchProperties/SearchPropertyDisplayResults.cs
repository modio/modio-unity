using System;
using Modio.Errors;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyDisplayResults : ISearchProperty
    {
        [SerializeField] ModioUIGroup _modGroup;

        [SerializeField, Tooltip("(Optional) Enable this gameObject when there are zero results")]
        GameObject _displayWhenNoResults;
        [SerializeField,
         Tooltip("(Optional) Enable this gameObject when there are network issues and there's no results")]
        GameObject _displayWhenOffline;

        [SerializeField] UnityEvent<Error> _errorHandler;

        public void OnSearchUpdate(ModioUISearch search)
        {
            if (!search.IsSearching)
            {
                if (_modGroup != null) _modGroup.SetMods(search.LastSearchResultMods, search.LastSearchSelectionIndex);

                var handledNetworkError 
                    = _displayWhenOffline != null 
                      && search.LastSearchError.Code == ErrorCode.CANNOT_OPEN_CONNECTION;

                if (search.LastSearchError && !handledNetworkError)
                {
                    _errorHandler.Invoke(search.LastSearchError);
                }

                if (_displayWhenOffline != null)
                    _displayWhenOffline.SetActive(handledNetworkError && search.LastSearchResultMods.Count == 0);

                if (_displayWhenNoResults != null)
                    _displayWhenNoResults.SetActive(!handledNetworkError && search.LastSearchResultMods.Count == 0);
            }
            else
            {
                if (_displayWhenOffline != null) _displayWhenOffline.SetActive(false);

                if (_displayWhenNoResults != null) _displayWhenNoResults.SetActive(false);
            }
        }
    }
}
