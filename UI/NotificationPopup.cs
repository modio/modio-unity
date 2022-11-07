using System;
using System.Collections.Generic;
using System.Linq;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    /// <summary>
    /// Opens a dialogue window with a set of buttons.
    /// Example:
    /// NotificationPopup.Open("header", "text body",
    ///     new NotificationPopup.ButtonConfig("Okay", ()=> Debug.Log("okay")),
    ///     new NotificationPopup.ButtonConfig("Cancel", ()=> Debug.Log("cancel"))
    /// );
    /// </summary>
    class NotificationPopup : SimpleMonoSingleton<NotificationPopup>
    {
        public class ButtonConfig
        {
            public string name;
            public Action action;

            public ButtonConfig(string name, Action action)
            {
                this.name = name;
                this.action = action;
            }
        }

        public TextMeshProUGUI header, body;
        public List<NotificationPopupButton> buttons;

        //Split this out into a nintendo partial
        //alternatively just call it from directly in that code, its just a wrapper
        public static void ErrorNintendoDiscSpace() =>
            Instance.Open("Error",
                "This device does not have enough hard disk space to install this mod.",
                new ButtonConfig("Okay", null));

        protected override void Awake()
        {
            base.Awake();
            Hide();
        }

        public void Open(string header, string body, params ButtonConfig[] buttonConfigs)
        {
            this.header.text = header;
            this.body.text = body;

            buttons.ForEach(button => button.Hide());

            if(buttonConfigs.Count() > buttons.Count())
            {
                this.header.text = "Error";
                this.body.text = "This textbox is unable to display the input configuration.";

                buttons[0].Set(new ButtonConfig("Error",
                    () => Debug.LogWarning("There are not enough buttons to display these choices.")),
                    this);

                throw new NotImplementedException("Error. Contact modio."); 
            }

            for(int i = 0; i < buttonConfigs.Count(); i++)
            {
                buttons[i].Set(buttonConfigs[i], this);
            }

            Show();
        }

        private void Show()
        {            
            gameObject.SetActive(true);
            SelectionManager.Instance.SelectView(UiViews.NotificationPopup);
        }

        private void Hide() => gameObject.SetActive(false);

        public void Close()
        {
            Hide();
            SelectionManager.Instance.SelectPreviousView();
        }
    }
}

