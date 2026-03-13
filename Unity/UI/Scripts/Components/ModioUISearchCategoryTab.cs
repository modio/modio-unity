using System;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components
{
    /// <summary>
    /// A "tab" button that allows selecting a search category.
    /// Used in the menu at the top of TUI, as well as the radio buttons on the Library tab
    /// </summary>
    public class ModioUISearchCategoryTab : MonoBehaviour
    {
        public bool IsSelected => _toggle?.isOn ?? false;
        
        [SerializeField] ModioUISearchSettings _search;
        [SerializeField] TMP_Text _label;
        [SerializeField] ModioUILocalizedText _labelLocalised;
        [SerializeField] Image _image;

        [SerializeField] bool _selectOnEnable;
        [SerializeField] bool _useOverrideIfSet;
        [SerializeField] int _useOverrideIfSetIndex;

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

        void OnEnable()
        {
            if (_useOverrideIfSet &&
                ModioServices.TryResolve(out ModioSettings settings) &&
                settings.TryGetPlatformSettings(out ModioUISearchCategoryTabOverrideSettings overrideSettings))
            {
                if (_useOverrideIfSetIndex >= 0 &&
                    _useOverrideIfSetIndex < overrideSettings.OverrideMainSearchesTo.Length)
                {
                    gameObject.SetActive(true);
                    SetSearch(overrideSettings.OverrideMainSearchesTo[_useOverrideIfSetIndex]);
                    if (_toggle.isOn)
                        OnToggleValueChanged(true);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
                
        }

        void OnToggleValueChanged(bool newValue)
        {
            if (newValue && ModioUISearch.Default != null && _search != null)
            {
                _search.SetAsCustomSearchBase(ModioUISearch.Default);
                _search.Search(ModioUISearch.Default);
            }
        }

        public void SetSearchAndIndex(ModioUISearchSettings search, int index)
        {
            _useOverrideIfSetIndex = index;
            SetSearch(search);
            
            if (_toggle.isOn)
                OnToggleValueChanged(true);
        }

        public void SetSearch(ModioUISearchSettings searchSettings)
        {
            _search = searchSettings;
            if (_label != null) _label.text = searchSettings.DisplayAs;
            if (_labelLocalised != null) _labelLocalised.SetKey(searchSettings.DisplayAsLocalisedKey);

            if (_image != null)
            {
                _image.sprite = searchSettings.Icon;
                _image.gameObject.SetActive(searchSettings.Icon != null);
            }
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
