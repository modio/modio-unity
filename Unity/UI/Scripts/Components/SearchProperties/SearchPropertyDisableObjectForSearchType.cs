using System;
using System.Linq;
using Modio.Unity.UI.Search;
using UnityEngine;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyDisableObjectForSearchType : ISearchProperty
    {
        [SerializeField] GameObject[] _gameObjectsToHide;
        [SerializeField] GameObject[] _gameObjectsToShow;

        [SerializeField] bool _hideOnCustomSearch;
        [SerializeField] SpecialSearchType[] _hideForSearchTypes;

        public void OnSearchUpdate(ModioUISearch search)
        {
            bool searchConditionsMet = (_hideOnCustomSearch && search.HasCustomSearch()) ||
                                       _hideForSearchTypes.Contains(search.LastSearchPreset);

            foreach (GameObject gameObject in _gameObjectsToHide)
            {
                gameObject.SetActive(!searchConditionsMet);
            }

            foreach (GameObject gameObject in _gameObjectsToShow)
            {
                gameObject.SetActive(searchConditionsMet);
            }
        }
    }
}
