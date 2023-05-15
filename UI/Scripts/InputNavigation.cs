using System.Collections.Generic;
using ModIO.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModIOBrowser
{
    //Split into input and navigation?
    //Yes its doing two things
    class InputNavigation : SelfInstancingMonoSingleton<InputNavigation>
    {        
        [SerializeField] List<GameObject> ControllerButtonIcons = new List<GameObject>();
        [SerializeField] List<GameObject> MouseButtonIcons = new List<GameObject>();

        //Set this when we detect mouse behaviour so we can disable certain controller behaviours
        public bool mouseNavigation = false;

        public void SetToController()
        {
            mouseNavigation = false;
            Cursor.lockState = CursorLockMode.Locked;

            //Reselect a ui component in case the mouse has moved off
            SelectionManager.Instance.SelectMostRecentStillActivatedUiItem();

            ShowControllerButtonIconsAndHideMouseButtonIcons();
        }

        public void SetToMouse()
        {
            Cursor.lockState = CursorLockMode.None;
            HideControllerButtonIconsAndShowMouseButtonIcons();
            mouseNavigation = true;
        }

        void ShowControllerButtonIconsAndHideMouseButtonIcons()
        {
            foreach(GameObject icon in ControllerButtonIcons)
            {
                icon?.SetActive(true);
            }
            foreach(GameObject icon in MouseButtonIcons)
            {
                icon?.SetActive(false);
            }
        }

        void HideControllerButtonIconsAndShowMouseButtonIcons()
        {
            foreach(GameObject icon in ControllerButtonIcons)
            {
                icon?.SetActive(false);
            }
            foreach(GameObject icon in MouseButtonIcons)
            {
                icon?.SetActive(true);
            }
        }

        public void DeselectUiGameObject()
        {
            if(!mouseNavigation)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        public void SelectGameObject(GameObject go)
        {
            if(!Browser.Instance.BrowserCanvas.activeSelf)
            {
                return;
            }

            if(!mouseNavigation)
            {
                EventSystem.current.SetSelectedGameObject(go);
            }
        }

        public void Select(Selectable s, bool selectEvenWhenUsingMouse = false)
        {
            if(!Browser.Instance.BrowserCanvas.activeSelf || s == null)
            {
                return;
            }

            if(!mouseNavigation || selectEvenWhenUsingMouse)
            {
                EventSystem.current.SetSelectedGameObject(null, null);
                EventSystem.current.SetSelectedGameObject(s.gameObject, null);
            }
        }
    }
}
