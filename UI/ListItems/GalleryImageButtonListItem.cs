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
        private Color _normalColorDefault;

        protected override void Awake()
        {
            base.Awake();
            this._normalColorDefault = this.button.colors.normalColor;
        }

        public override void Setup(Action clicked)
        {
            base.Setup();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked());
            gameObject.SetActive(true);
        }

        //Uses the selected button colors as the normal color.
        //This allows the button to appear to be "Selected"
        public override void Select()
        {
            ColorBlock colorVar = button.colors;
            colorVar.normalColor = button.colors.selectedColor;
            button.colors = colorVar;
        }

        //Returns the normal button colors to it's default
        //This removes the "Selected" button appearance
        public override void DeSelect()
        {
            ColorBlock colorVar = button.colors;
            colorVar.normalColor = this._normalColorDefault;
            button.colors = colorVar;
        }
    }
}
