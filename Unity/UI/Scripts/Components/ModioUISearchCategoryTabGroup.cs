using System.Collections.Generic;
using Modio.Unity.Settings;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUISearchCategoryTabGroup : MonoBehaviour
    {
        [SerializeField] ModioUISearchCategoryTab _firstTab;

        [SerializeField] GameObject _disableIfNoCategory;

        [SerializeField] TMP_Text _categoryName;
        [SerializeField] ModioUILocalizedText _categoryNameLocalized;

        readonly List<ModioUISearchCategoryTab> _tabs = new List<ModioUISearchCategoryTab>();

        bool _hasRunStart;
        int _activeTabCount;
        int _setCategoryOnFrame;

        public void ClearCategory()
        {
            if(_setCategoryOnFrame != Time.frameCount)
                SetCategory(null);
        }

        public void SetCategory(ModioUISearchCategory category)
        {
            if (_disableIfNoCategory != null) _disableIfNoCategory.SetActive(category != null);

            if(category == null) return;

            _setCategoryOnFrame = Time.frameCount;

            SetTabs(category.Tabs);

            if (ModioUISearch.Default != null)
            {
                if (category.CustomSearchBase != null)
                    category.CustomSearchBase.SetAsCustomSearchBase(ModioUISearch.Default);
                else
                {
                    ModioUISearch.Default.SetCustomSearchBase(null, default);
                }
            }

            if (_categoryName != null) _categoryName.text = category.CategoryLabel;
            if (_categoryNameLocalized != null) _categoryNameLocalized.SetKey(category.CategoryLabelLocalized);
        }

        void Start()
        {
            if (_tabs.Count > 0) _tabs[0].SetSelected();
            else if (_disableIfNoCategory != null) _disableIfNoCategory.SetActive(false);
            if (_activeTabCount == 1) _tabs[0].gameObject.SetActive(false);
            _hasRunStart = true;
        }

        public void SetTabs(IEnumerable<ModioUISearchSettings> tabSearches)
        {
            int index = 0;

            var compUISettings = ModioClient.Settings.GetPlatformSettings<ModioComponentUISettings>();
            bool showMonetizationUI = compUISettings is { ShowMonetizationUI: true, };
                
            foreach (ModioUISearchSettings search in tabSearches)
            {
                if (!showMonetizationUI && search.HiddenIfMonetizationDisabled)
                {
                    continue;
                }

                ModioUISearchCategoryTab currentTab;

                if (index < _tabs.Count)
                {
                    currentTab = _tabs[index];
                    currentTab.gameObject.SetActive(true);
                }
                else if (index == 0)
                {
                    _tabs.Add(_firstTab);
                    currentTab = _firstTab;
                }
                else
                {
                    currentTab = Instantiate(_firstTab, _firstTab.transform.parent);
                    currentTab.transform.SetSiblingIndex(_firstTab.transform.GetSiblingIndex() + index);
                    _tabs.Add(currentTab);
                }

                currentTab.SetSearch(search);

                index++;
            }

            _activeTabCount = index;

            for (; index < _tabs.Count; index++)
            {
                _tabs[index].gameObject.SetActive(false);
            }

            //We might be setting this up before other systems are ready
            // Delay until start if appropriate
            if (_hasRunStart)
            {
                // Set selected for all in the group, not just the first,
                // as group logic doesn't run if the group's gameobject is inactive
                for (var i = 0; i < _tabs.Count; i++)
                {
                    _tabs[i].SetSelected(i == 0);
                }
                if (_activeTabCount == 1) _tabs[0].gameObject.SetActive(false);
            }

            if (_disableIfNoCategory != null && _activeTabCount <= 1) _disableIfNoCategory.SetActive(false);
        }
    }
}
