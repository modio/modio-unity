using UnityEngine;

namespace ModIOBrowser.Implementation
{
    internal class SelectionOverlayHandler : SimpleMonoSingleton<SelectionOverlayHandler>
    {
        [Header("Selection Overlay Objects")]
        [SerializeField] BrowserModListItem_Overlay BrowserModListItemOverlay;
        public SearchResultListItem_Overlay SearchResultListItemOverlay;
        [SerializeField] GameObject CollectionListItemOverlay;
        [SerializeField] GameObject SearchModListItemOverlay;

        public void SetBrowserModListItemOverlayActive(bool state)
        {
            BrowserModListItemOverlay?.gameObject.SetActive(state);
        }

        public static bool TryToOpenMoreOptionsForBrowserOverlayObject()
        {
            if(Instance.BrowserModListItemOverlay.gameObject.activeSelf)
            {
                Instance.BrowserModListItemOverlay.ShowMoreOptions();
                return true;
            }
            return false;
        }

        public static bool TryToOpenMoreOptionsForSearchResultsOverlayObject()
        {
            if(Instance.SearchResultListItemOverlay.gameObject.activeSelf)
            {
                Instance.SearchResultListItemOverlay.ShowMoreOptions();
                return true;
            }
            return false;
        }

        public static bool TryAlternateForBrowserOverlayObject()
        {
            if(Instance.BrowserModListItemOverlay.gameObject.activeSelf)
            {
                Instance.BrowserModListItemOverlay.SubscribeButton();
                return true;
            }
            return false;
        }

        public static bool TryAlternateForSearchResultsOverlayObject()
        {
            if(Instance.SearchResultListItemOverlay.gameObject.activeSelf)
            {
                Instance.SearchResultListItemOverlay.SubscribeButton();
                return true;
            }
            return false;
        }

        public void MoveSelection(BrowserModListItem listItem)
        {
            BrowserModListItemOverlay.Setup(listItem);
        }

        public void MoveSelection(SearchResultListItem listItem)
        {
            SearchResultListItemOverlay.Setup(listItem);
        }

        public void Deselect(BrowserModListItem listItem)
        {
            // If the context menu is open, dont hide the overlay
            if(Browser.Instance.contextMenu.activeSelf)
            {
                return;
            }
            if(BrowserModListItemOverlay != null
               && BrowserModListItemOverlay.listItemToReplicate == listItem
               && !Browser.mouseNavigation)
            {
                BrowserModListItemOverlay?.gameObject.SetActive(false);
            }
        }

        public void Deselect(SearchResultListItem listItem)
        {
            // If the context menu is open, dont hide the overlay
            if(Browser.Instance.contextMenu.activeSelf)
            {
                return;
            }
            if(SearchResultListItemOverlay != null
               && SearchResultListItemOverlay.listItemToReplicate == listItem
               && !Browser.mouseNavigation)
            {
                SearchResultListItemOverlay?.gameObject.SetActive(false);
            }
        }
    }
}
