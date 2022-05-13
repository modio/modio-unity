using System.Collections;
using System.Collections.Generic;
using ModIO;
using ModIO.Implementation;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    /// <summary>
    ///the main interface for interacting with the Mod Browser UI
    /// </summary>
    public partial class Browser
    {
        [Header("Search Panel")]
        [SerializeField] GameObject SearchPanel;
        [SerializeField] TMP_InputField SearchPanelField;
        [SerializeField] GameObject SearchPanelTagCategoryPrefab;
        [SerializeField] RectTransform SearchPanelTagViewport;
        [SerializeField] Transform SearchPanelTagParent;
        [SerializeField] GameObject SearchPanelTagPrefab;
        [SerializeField] Selectable SearchPanelDefaultSelection;
        [SerializeField] Image SearchPanelLeftBumperIcon;
        [SerializeField] Image SearchPanelRightBumperIcon;

        internal static HashSet<Tag> searchFilterTags = new HashSet<Tag>();
        TagCategory[] tags;
        
#region Search Panel

        public void OpenSearchPanel()
        {
            SearchPanel.SetActive(true);
            SearchPanelField.text = "";
            SearchPanelDefaultSelection.Select();
            //ScrollRectViewHandler.Instance.CurrentViewportContent = SearchPanelTagParent;
            SetupSearchPanelTags();
        }

        public void CloseSearchPanel()
        {
            SearchPanel.SetActive(false);
        }

        public void ClearSearchFilter()
        {
            searchFilterTags = new HashSet<Tag>();
            SearchPanelField.SetTextWithoutNotify("");
            SetupSearchPanelTags();
        }

        internal void SetupSearchPanelTags()
        {
            if(tags != null)
            {
                CreateTagCategoryListItems(tags);
            }
            else
            {
                ModIOUnity.GetTagCategories(GetTags);
            }
        }

        internal void GetTags(ResultAnd<TagCategory[]> resultAndTags)
        {
            if(resultAndTags.result.Succeeded())
            {
                this.tags = resultAndTags.value;
                CreateTagCategoryListItems(resultAndTags.value);
            }
        }

        internal void CreateTagCategoryListItems(TagCategory[] tags)
        {
            if(tags == null || tags.Length < 1)
            {
                return;
            }

            ListItem.HideListItems<TagListItem>();
            ListItem.HideListItems<TagCategoryListItem>();
            TagJumpToSelection.ClearCache();

            foreach(TagCategory category in tags)
            {
                ListItem categoryListItem = ListItem.GetListItem<TagCategoryListItem>(SearchPanelTagCategoryPrefab, SearchPanelTagParent, colorScheme);
                categoryListItem.Setup(category.name);

                CreateTagListItems(category);
            }
            UpdateSearchPanelBumperIcons();
            LayoutRebuilder.ForceRebuildLayoutImmediate(SearchPanelTagParent as RectTransform);
        }

        void CreateTagListItems(TagCategory category)
        {
            //SearchPanelCurrentTagCategoryTitle.text = selectedTagCategory;
            bool setJumpTo = false;
            
            foreach(ModIO.Tag tag in category.tags)
            {
                ListItem tagListItem = ListItem.GetListItem<TagListItem>(SearchPanelTagPrefab, SearchPanelTagParent, colorScheme);
                tagListItem.Setup(tag.name, category.name);
                tagListItem.SetViewportRestraint(SearchPanelTagParent as RectTransform, SearchPanelTagViewport);
                
                if(!setJumpTo)
                {
                    tagListItem.Setup();
                    setJumpTo = true;
                }
            }
        }

        public void ApplySearchFilter()
        {
            OpenSearchResults(SearchPanelField.text);
        }

#endregion

    }
}
