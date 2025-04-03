using Modio.Mods;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUIFilterTagCategory : MonoBehaviour
    {
        [SerializeField] TMP_Text _categoryTitle;

        [SerializeField] TMP_Text _filterCount;
        [SerializeField] GameObject _filterCountBackground;
        public int CurrentFilterCount { get; private set; }

        void Reset()
        {
            _categoryTitle = GetComponentInChildren<TMP_Text>();
        }

        public void Setup(GameTagCategory category)
        {
            _categoryTitle.text = category.Name;
            SetFilterCount(0);
        }

        public void SetFilterCount(int filterCount)
        {
            CurrentFilterCount = filterCount;
            if (_filterCount != null) _filterCount.text = filterCount.ToString();

            if (_filterCountBackground != null) _filterCountBackground.SetActive(filterCount > 0);
        }
    }
}
