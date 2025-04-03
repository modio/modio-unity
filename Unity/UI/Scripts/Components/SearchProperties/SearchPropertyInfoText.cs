using System;
using Modio.API;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Search;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserProfile = Modio.Users.UserProfile;

namespace Modio.Unity.UI.Components.SearchProperties
{
    [Serializable]
    public class SearchPropertyInfoText : ISearchProperty
    {
        [SerializeField] TMP_Text _searchText;
        [SerializeField] GameObject _disableWhileShowingCustomText;
        [SerializeField] GameObject _showWhileShowingCustomText;

        [SerializeField] TMP_Text _searchCategoryName;
        [SerializeField] ModioUILocalizedText _searchCategoryNameLocalized;
        [SerializeField] Image _searchCategoryIcon;

        public void OnSearchUpdate(ModioUISearch search)
        {
            var filter = search.LastSearchFilter;

            var searchPhrases = filter.GetSearchPhrase(Filtering.Like);
            var searchHasEntries = searchPhrases?.Count > 0;

            if (_searchText != null)
            {
                _searchText.enabled = searchHasEntries;
                if (searchHasEntries) _searchText.text = $"{string.Join(" ", searchPhrases)}";

                if (SpecialSearchType.SearchForTag == search.LastSearchPreset)
                {
                    _searchText.enabled = true;
                    var tags = filter.GetTags();
                    _searchText.text = $"{string.Join(" ", tags)}";
                }
            }

            var users = filter.GetUsers();
            bool searchHasUser = false;

            if (users.Count > 0)
            {
                UserProfile searchUser = users[0];
                searchHasUser = searchUser != null;

                if (_searchText != null)
                {
                    _searchText.enabled = searchHasUser;
                    if (searchHasUser) _searchText.text = $"{searchUser.Username}";
                }
            }

            if (_disableWhileShowingCustomText != null)
                _disableWhileShowingCustomText.SetActive(!(searchHasEntries || searchHasUser));

            if (_showWhileShowingCustomText != null)
                _showWhileShowingCustomText.SetActive((searchHasEntries || searchHasUser));

            if (search.LastSearchSettingsFrom != null)
            {
                if (_searchCategoryName != null) _searchCategoryName.text = search.LastSearchSettingsFrom.DisplayAs;

                if (_searchCategoryNameLocalized != null)
                    _searchCategoryNameLocalized.SetKey(search.LastSearchSettingsFrom.DisplayAsLocalisedKey);

                if (_searchCategoryIcon != null)
                {
                    _searchCategoryIcon.sprite = search.LastSearchSettingsFrom.Icon;
                    _searchCategoryIcon.enabled = search.LastSearchSettingsFrom.Icon != null;
                }
            }
        }
    }
}
