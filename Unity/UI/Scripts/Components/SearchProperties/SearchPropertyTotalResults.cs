using System;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyTotalResults : ISearchProperty
    {
        [SerializeField] TMP_Text _foundResultsText;
        [SerializeField] string _foundResultsString = "Found {0} results";

        public void OnSearchUpdate(ModioUISearch search)
        {
            _foundResultsText.gameObject.SetActive(search.LastSearchResultModCount > 0);

            _foundResultsText.text = string.Format(_foundResultsString, search.LastSearchResultModCount);
        }
    }
}
