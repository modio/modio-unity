using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// This is for the little pips on the bottom of the gallery image slide. Each pip represents a
    /// different gallery image you can select and view.
    /// </summary>
    internal class GalleryImageButtonListItem : ListItem
    {
        [SerializeField] Button button;

        public override void Setup(Action clicked)
        {
            base.Setup();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked());
            gameObject.SetActive(true);
        }
    }
}
