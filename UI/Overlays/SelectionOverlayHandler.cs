using UnityEngine;

namespace ModIOBrowser.Implementation
{
    internal class SelectionOverlayHandler : MonoBehaviour
    {
        [Header("Selection Overlay Objects")]
        [SerializeField] BrowserModListItem_Overlay BrowserModListItemOverlay;
        [SerializeField] SearchResultListItem_Overlay SearchResultListItemOverlay;
        [SerializeField] GameObject CollectionListItemOverlay;
        [SerializeField] GameObject SearchModListItemOverlay;

        // Singleton
        public static SelectionOverlayHandler Instance;
        
        void Awake()
        {
            Instance = this;
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
            if(BrowserModListItemOverlay != null)
            {
                if(BrowserModListItemOverlay.listItemToReplicate == listItem)
                {
                    // Not sure why this was a problem but replaced with IPointerExit in the _Overlay class
                    //BrowserModListItemOverlay?.Hide();
                }
            }
        }

        public void Deselect(SearchResultListItem listItem)
        {
            // If the context menu is open, dont hide the overlay
            if(Browser.Instance.contextMenu.activeSelf)
            {
                return;
            }
            if(SearchResultListItemOverlay != null)
            {
                if(SearchResultListItemOverlay.listItemToReplicate == listItem)
                {
                    // Not sure why this was a problem but replaced with IPointerExit in the _Overlay class
                    //SearchResultListItemOverlay?.Hide();
                }
            }
        }
    }
}
