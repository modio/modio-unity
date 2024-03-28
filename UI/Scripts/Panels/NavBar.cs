using System.Collections;
using ModIO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ModIO.Util;

namespace ModIOBrowser.Implementation
{
    public class NavBar : SelfInstancingMonoSingleton<NavBar>
    {
        [Header("Nav Bar")]
        [SerializeField] TMP_Text BrowserPanelNavButton;
        [SerializeField] GameObject BrowserPanelNavButtonHighlights;
        [SerializeField] Image BrowserPanelHeaderBackground;
        [SerializeField] TMP_Text CollectionPanelNavButton;
        [SerializeField] GameObject CollectionPanelNavButtonHighlights;
        IEnumerator browserHeaderTransition;

        /// <summary>
        /// This is simply an On/Off state of the collection/browser buttons at the top of the UI
        /// panel to go between the two corresponding menus. The display is based on which menu you
        /// are currently in.
        /// </summary>
        internal void UpdateNavbarSelection()
        {
            if(Collection.IsOn())
            {
                Color col = CollectionPanelNavButton.color;
                col.a = 1f;
                CollectionPanelNavButton.color = col;
                CollectionPanelNavButtonHighlights.SetActive(true);

                col = BrowserPanelNavButton.color;
                col.a = 0.5f;
                BrowserPanelNavButton.color = col;
                BrowserPanelNavButtonHighlights.SetActive(false);
            }
            else
            {
                Color col = CollectionPanelNavButton.color;
                col.a = 0.5f;
                CollectionPanelNavButton.color = col;
                CollectionPanelNavButtonHighlights.SetActive(false);

                col = BrowserPanelNavButton.color;
                col.a = 1f;
                BrowserPanelNavButton.color = col;
                BrowserPanelNavButtonHighlights.SetActive(true);
            }
        }
    }
}
