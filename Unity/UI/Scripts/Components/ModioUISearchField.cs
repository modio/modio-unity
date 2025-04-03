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
            searchField.text = lastSearchPhrase = "";
            ModioUISearch.Default.AppliedSearchPreset += OnAppliedSearchPreset;
        }

        void OnDisable()
        {
            ModioUISearch.Default.AppliedSearchPreset -= OnAppliedSearchPreset;
        }

        void OnAppliedSearchPreset()
        {
            searchField.text = lastSearchPhrase = "";
        }

        public void FilterView()
        {
            if (lastSearchPhrase == searchField.text) return;
            lastSearchPhrase = searchField.text;

            ModioUISearch.Default.ApplySearchPhrase(searchField.text);
        }
    }
}
