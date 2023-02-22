using System;
using TMPro;
using UnityEngine;

namespace ModIOBrowser.Implementation
{
    class NotificationPopupButton : MonoBehaviour
    {
        public TextMeshProUGUI buttonName;
        private Action action;
        private NotificationPopup master;

        public void Set(NotificationPopup.ButtonConfig config, NotificationPopup master)
        {
            buttonName.text = config.name;
            this.action = config.action;
            this.master = master;
            gameObject.SetActive(true);            
        }

        public void OnClick()
        {
            action?.Invoke();
            master.Close();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}

