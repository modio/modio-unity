using ModIO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// This is used for the TagListItem that is populated when opening the search panel
    /// </summary>
    internal class TagListItem : ListItem
    {
        [SerializeField] TMP_Text title;
        [SerializeField] Toggle toggle;
        TagJumpToSelection jumpToComponent;
        public RectTransform rectTransform;

        public string tagName;
        public string tagCategory;

        void OnEnable()
        {
            rectTransform = transform as RectTransform;
        }

#region Overrides
        public override void SetViewportRestraint(RectTransform content, RectTransform viewport)
        {
            base.SetViewportRestraint(content, viewport);
            
            viewportRestraint.PercentPaddingVertical = 0.15f;
        }

        public override void Setup(string tagName, string tagCategory)
        {
            this.tagName = tagName;
            this.tagCategory = tagCategory;
            
            title.text = tagName;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            toggle.onValueChanged.RemoveAllListeners();

            toggle.isOn = IsTagSelected(tagName);

            toggle.onValueChanged.AddListener(Toggled);

            if(jumpToComponent != null)
            {
                Destroy(jumpToComponent);
            }
        }

        // This method is used for setting up the Free / Premium filter options (not connected to actual tags)
        public override void Setup(RevenueType revenueType)
        {
            title.text = revenueType == RevenueType.Free 
                ? TranslationManager.Instance.Get("Free") 
                : TranslationManager.Instance.Get("Premium");
            
            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            toggle.onValueChanged.RemoveAllListeners();

            SearchPanel.searchFilterFree = true;
            SearchPanel.searchFilterPremium = true;
            toggle.isOn = true;

            toggle.onValueChanged.AddListener(x =>
            {
                if (revenueType == RevenueType.Free)
                    SearchPanel.searchFilterFree = x;
                
                if (revenueType == RevenueType.Paid)
                    SearchPanel.searchFilterPremium = x;
            });

            // This might exist if we recycled this object.
            // We destroy it because the other Setup() method gets invoked after this, which will set it up
            if(jumpToComponent != null)
            {
                Destroy(jumpToComponent);
            }
        }

        /// <summary>
        /// Use this for setting up the JumpTo component on selection
        /// </summary>
        public override void Setup()
        {
            jumpToComponent = gameObject.AddComponent<TagJumpToSelection>();
            jumpToComponent.selection = selectable;
            jumpToComponent.Setup();
        }
#endregion // Overrides
        
        public void Toggled(bool isOn)
        {
            Tag tag = new Tag(tagCategory, tagName);
            
            if(isOn)
            {
                if (!SearchPanel.searchFilterTags.Contains(tag))
                {
                    SearchPanel.searchFilterTags.Add(tag);
                }
            }
            else
            {
                if (SearchPanel.searchFilterTags.Contains(tag))
                {
                    SearchPanel.searchFilterTags.Remove(tag);
                }
            }
        }

        bool IsTagSelected(string tagName)
        {
            foreach(Tag tag in SearchPanel.searchFilterTags)
            {
                if(tag.name == tagName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
