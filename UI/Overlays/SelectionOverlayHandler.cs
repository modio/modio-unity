using ModIO;
using ModIO.Util;
using UnityEngine;

namespace ModIOBrowser.Implementation
{
    internal class SelectionOverlayHandler : SelfInstancingMonoSingleton<SelectionOverlayHandler>
    {
        [Header("Selection Overlay Objects")]
        [SerializeField] HomeModListItem_Overlay homeModListItemOverlay;
        public SearchResultListItem_Overlay SearchResultListItemOverlay;
        [SerializeField] GameObject CollectionListItemOverlay;
        [SerializeField] GameObject SearchModListItemOverlay;

        public void SetBrowserModListItemOverlayActive(bool state)
        {
            homeModListItemOverlay?.gameObject.SetActive(state);
        }

        public static bool TryToOpenMoreOptionsForBrowserOverlayObject()
        {
            if(Instance.homeModListItemOverlay.gameObject.activeSelf)
            {
                Instance.homeModListItemOverlay.ShowMoreOptions();
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
            if(Instance.homeModListItemOverlay.gameObject.activeSelf)
            {
                Instance.homeModListItemOverlay.SubscribeButton();
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

        public void MoveSelection(HomeModListItem listItem)
        {
            homeModListItemOverlay.Setup(listItem);
        }

        public void MoveSelection(SearchResultListItem listItem)
        {
            SearchResultListItemOverlay.Setup(listItem);
        }

        public void Deselect(HomeModListItem listItem)
        {
            // If the context menu is open, dont hide the overlay
            if(ModioContextMenu.Instance.ContextMenu.activeSelf)
            {
                return;
            }
            if(homeModListItemOverlay != null
               && homeModListItemOverlay.listItemToReplicate == listItem
               && !InputNavigation.Instance.mouseNavigation)
            {
                homeModListItemOverlay?.gameObject.SetActive(false);
            }
        }

        public void Deselect(SearchResultListItem listItem)
        {
            // If the context menu is open, dont hide the overlay
            if(ModioContextMenu.Instance.ContextMenu.activeSelf)
            {
                return;
            }
            if(SearchResultListItemOverlay != null
               && SearchResultListItemOverlay.listItemToReplicate == listItem
               && !InputNavigation.Instance.mouseNavigation)
            {
                SearchResultListItemOverlay?.gameObject.SetActive(false);
            }
        }
    }
}
