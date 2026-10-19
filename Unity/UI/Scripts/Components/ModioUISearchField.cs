using System;
using System.Collections.Generic;
using Modio.API;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUISearchField : MonoBehaviour
    {
        [SerializeField] TMP_InputField searchField;
        string lastSearchPhrase;
        bool _hasRunStart;

        void Start()
        {
            //delay first call of OnEnable until Start, to ensure ModioUISearch.Default has had time to be set
            _hasRunStart = true;
            OnEnable();
        }

        void OnEnable()
        {
            if (!_hasRunStart) return;
            
            IList<string> searchPhrases = ModioUISearch.Default.ModioSearch.LastSearchFilter?.GetSearchPhrase(Filtering.Like);
            if(searchPhrases?.Count > 0)
                searchField.text = lastSearchPhrase = searchPhrases[0];
            else
                searchField.text = lastSearchPhrase = "";

            ModioUISearch.Default.ModioSearch.OnSearchUpdated -= OnSearchUpdated;
            ModioUISearch.Default.ModioSearch.OnSearchUpdated += OnSearchUpdated;
        }

        void OnDisable()
        {
            ModioUISearch.Default.ModioSearch.OnSearchUpdated -= OnSearchUpdated;
        }

        void OnSearchUpdated()
        {
            IList<string> searchPhrases = ModioUISearch.Default.ModioSearch.LastSearchFilter.GetSearchPhrase(Filtering.Like);
            if(searchPhrases?.Count == 0)
                searchField.text = lastSearchPhrase = "";
        }

        public void FilterView()
        {
            if (lastSearchPhrase == searchField.text) return;
            lastSearchPhrase = searchField.text;

            ModioUISearch.Default.ModioSearch.ApplySearchPhrase(searchField.text);
        }
    }
}
