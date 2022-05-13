using UnityEngine;
using TMPro;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// These are titles placed between the different TagListItems to show their category groupings
    /// </summary>
    internal class TagCategoryListItem : ListItem
    {
        [SerializeField] TMP_Text title;

        public override void Setup(string tagName)
        {
            base.Setup();
            title.text = tagName;
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
        }
    }
}
