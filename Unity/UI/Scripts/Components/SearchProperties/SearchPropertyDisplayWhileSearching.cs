using System;
using Modio.Unity.UI.Search;
using UnityEngine;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyDisplayWhileSearching : ISearchProperty
    {
        enum AdditiveLoadBehaviour
        {
            Ignore,
            OnlyDuringAdditiveLoad,
            HideDuringAdditiveLoad,
        }
        
        [SerializeField] GameObject _displayWhileSearching;
        [SerializeField] AdditiveLoadBehaviour _additiveLoadBehaviour;
        
        public void OnSearchUpdate(ModioUISearch search)
        {
            bool matchesAdditiveBehaviour = _additiveLoadBehaviour switch
            {
                AdditiveLoadBehaviour.Ignore                 => true,
                AdditiveLoadBehaviour.OnlyDuringAdditiveLoad => search.ModioSearch.IsAdditiveSearch,
                AdditiveLoadBehaviour.HideDuringAdditiveLoad => !search.ModioSearch.IsAdditiveSearch,
                _                                            => throw new ArgumentOutOfRangeException()
            };
            _displayWhileSearching.SetActive(search.ModioSearch.IsSearching && matchesAdditiveBehaviour);
        }
    }
}
