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
            base.Setup();

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
                if (!Browser.searchFilterTags.Contains(tag))
                {
                    Browser.searchFilterTags.Add(tag);
                }
            }
            else
            {
                if (Browser.searchFilterTags.Contains(tag))
                {
                    Browser.searchFilterTags.Remove(tag);
                }
            }
        }

        bool IsTagSelected(string tagName)
        {
            foreach(Tag tag in Browser.searchFilterTags)
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
