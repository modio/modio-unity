using System;
using System.Collections;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyEndlessScroll : ISearchProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] ScrollRect _scrollRect;
        [SerializeField] float _distanceFromBottomToLoadContent = 300f;

        Coroutine _monitorCoroutine;
        ModioUISearch _search;
        bool _hasRunStart;

        public void OnSearchUpdate(ModioUISearch search)
        {
            _search = search;
        }

        public void Start()
        {
            _hasRunStart = true;
            OnEnable();
        }

        public void OnDestroy() { }

        public void OnEnable()
        {
            if (!_hasRunStart) return;
            _monitorCoroutine = _scrollRect.StartCoroutine(MonitorCo());
        }

        IEnumerator MonitorCo()
        {
            while (true)
            {
                var scrollRectTransform = (RectTransform)_scrollRect.transform;

                var rectHeight = scrollRectTransform.rect.height;
                var distanceFromBottom = -(rectHeight + _scrollRect.content.offsetMin.y);

                if (distanceFromBottom < _distanceFromBottomToLoadContent &&
                    _search != null &&
                    _search.CanGetMoreMods &&
                    !_search.IsSearching)
                    _search.GetNextPageAdditivelyForLastSearch();

                yield return null;
            }
        }

        public void OnDisable()
        {
            if (_monitorCoroutine != null) _scrollRect.StopCoroutine(_monitorCoroutine);
        }
    }
}
