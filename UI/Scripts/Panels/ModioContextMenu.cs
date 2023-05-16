using ModIO.Util;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ModIOBrowser.Implementation
{
    class ModioContextMenu : SelfInstancingMonoSingleton<ModioContextMenu>
    {
        public GameObject ContextMenu;
        [SerializeField] public Transform ContextMenuList;
        [SerializeField] public GameObject ContextMenuListItemPrefab;
        [SerializeField] public Selectable ContextMenuPreviousSelection;

        /// <summary>
        /// This opens a context menu with the specified options 'options' and makes itself a child
        /// to the given transform so that it can be assigned to remain in front of an element.
        /// It will also use the parent as the origin of where the menu will spawn from.
        /// The pivot is (0.5f, 0f) x, y
        /// </summary>
        /// <param name="t"></param>
        /// <param name="options"></param>
        /// <param name="previousSelection"></param>
        internal void Open(Transform t, List<ContextMenuOption> options, Selectable previousSelection)
        {
            if(options.Count < 1)
            {
                // We can't open a context menu without any context options
                return;
            }

            Vector2 position = t.position;

            // This counteracts an odd edge case with pivots and vertical layout groups
            // @HACK if the height of context list items changes, this will also need to be adjusted
            position.y -= 24f;

            //resize the width fo the context menu
            if(t is RectTransform rt)
            {
                float width = rt.sizeDelta.x;
                RectTransform contextRect = transform as RectTransform;
                Vector2 size = contextRect.sizeDelta;
                size.x = width;
                contextRect.sizeDelta = size;
            }

            ListItem.HideListItems<ContextMenuListItem>();
            ContextMenuPreviousSelection = previousSelection;
            gameObject.SetActive(true);
            transform.position = position;
            bool selectionMade = false;

            Selectable lastSelection = null;
            Selectable optionToSelect = null;

            foreach(var option in options)
            {
                ListItem li = ListItem.GetListItem<ContextMenuListItem>(ContextMenuListItemPrefab, ContextMenuList, SharedUi.colorScheme);
                li.Setup(TranslationManager.Instance.Get(option.nameTranslationReference), option.action);
                li.SetColorScheme(SharedUi.colorScheme);

                // Setup custom navigation
                {
                    Navigation nav = li.selectable.navigation;
                    nav.mode = Navigation.Mode.Explicit;
                    nav.selectOnLeft = null;
                    nav.selectOnRight = null;
                    nav.selectOnUp = lastSelection;
                    nav.selectOnDown = null;
                    li.selectable.navigation = nav;
                }

                // if last selection != null, make this list item the 'down' selection for the previous
                if(lastSelection != null)
                {
                    Navigation nav = lastSelection.navigation;
                    nav.selectOnDown = li.selectable;
                    lastSelection.navigation = nav;
                }

                lastSelection = li.selectable;

                // if this is the first context option, make it selected
                if(!selectionMade)
                {
                    optionToSelect = li.selectable;
                    selectionMade = true;
                }
            }

            if(!InputNavigation.Instance.mouseNavigation)
            {
                SelectionManager.Instance.SetNewViewDefaultSelection(UiViews.ContextMenu, optionToSelect);
                SelectionManager.Instance.SelectView(UiViews.ContextMenu);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(ContextMenuList as RectTransform);
        }

        /// <summary>
        /// hides the context menu and attempts to move the selection to whatever it was prior to
        /// opening the context menu
        /// </summary>
        public void Close()
        {
            gameObject.SetActive(false);
            if(ContextMenuPreviousSelection != null)
            {
                InputNavigation.Instance.Select(Instance.ContextMenuPreviousSelection);
            }
        }

        void Update()
        {
            // we can move this to ModioContextMenu
            // Detect mouse outside of context menu to cleanup/close context menu
            if(gameObject.activeSelf)
            {
                // if we detect a scroll, left or right mouse click, check if mouse is inside context
                // menu bounds. If not, then close context menu
                if(IsMouseInUse())
                {
                    // check if the mouse is within the bounds of the contextMenu
                    RectTransform contextRect = transform as RectTransform;
                    Vector3 mousePositionLocalToRect = contextRect.InverseTransformPoint(Input.mousePosition);

                    if(!contextRect.rect.Contains(mousePositionLocalToRect))
                    {
                        gameObject.SetActive(false);
                        // if using mouse we dont close using the method CloseContextMenu() because
                        // we dont want to move the selection
                        // CloseContextMenu();
                    }
                }
            }
        }

        bool IsMouseInUse()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.scroll.y.ReadValue() != 0f;
#else
            return Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1) || Input.GetAxis("Mouse ScrollWheel") != 0f;
#endif
        }
    }
}
