using System;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyFilterCount : ISearchProperty
    {
        [SerializeField] TMP_Text _filterCount;
        [SerializeField] GameObject _filterCountBackground;

        public void OnSearchUpdate(ModioUISearch search)
        {
            var tagCount = search.LastSearchFilter.GetTags().Count;
            if (_filterCount != null) _filterCount.text = tagCount.ToString();

            if (_filterCountBackground != null) _filterCountBackground.SetActive(tagCount > 0);
        }
    }
}
