using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components
{
    public class ModioUISearchCategoryTab : MonoBehaviour
    {
        [SerializeField] ModioUISearchSettings _search;
        [SerializeField] TMP_Text _label;
        [SerializeField] ModioUILocalizedText _labelLocalised;

        [SerializeField] bool _selectOnEnable;

        Toggle _toggle;

        void Awake()
        {
            _toggle = GetComponent<Toggle>();

            _toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        void Start()
        {
            if (_toggle.isOn)
                OnToggleValueChanged(true);
        }

        void OnToggleValueChanged(bool newValue)
        {
            if (newValue && ModioUISearch.Default != null && _search != null)
            {
                _search.SetAsCustomSearchBase(ModioUISearch.Default);
                _search.Search(ModioUISearch.Default);
            }
        }

        public void SetSearch(ModioUISearchSettings searchSettings)
        {
            _search = searchSettings;
            if (_label != null) _label.text = searchSettings.DisplayAs;
            if (_labelLocalised != null) _labelLocalised.SetKey(searchSettings.DisplayAsLocalisedKey);
        }

        public void SetSelected(bool selected = true)
        {
            //allow calling this on a disabled child
            if (_toggle == null)
            {
                _toggle = GetComponent<Toggle>();
                _toggle.isOn = selected;
                OnToggleValueChanged(selected);
                return;
            }

            if (_toggle.isOn != selected)
            {
                _toggle.isOn = selected;
            }
            else
            {
                //Simulate it being clicked
                OnToggleValueChanged(selected);
            }
        }
    }
}
