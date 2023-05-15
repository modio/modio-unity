using System;
using System.Collections.Generic;
using System.Linq;
using ModIO.Util;
using TMPro;
using UnityEngine;

namespace ModIOBrowser.Implementation
{
    /// <summary>
    /// Opens a dialogue window with a set of buttons.
    /// Example:
    /// NotificationPopup.Open("header", "text body",
    ///     new NotificationPopup.ButtonConfig("Okay", ()=> Debug.Log("okay")),
    ///     new NotificationPopup.ButtonConfig("Cancel", ()=> Debug.Log("cancel"))
    /// );
    /// </summary>
    class NotificationPopup : SelfInstancingMonoSingleton<NotificationPopup>
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

#pragma warning disable 0649 //These variables are infact allocated!
        private Translation headerTranslation, bodyTranslation;
#pragma warning restore 0649

        ////Split this out into a nintendo partial
        ////alternatively just call it from directly in that code, its just a wrapper
        //public static void ErrorNintendoDiscSpace() =>
        //    Instance.Open("Error",
        //        "This device does not have enough hard disk space to install this mod.",
        //        new ButtonConfig("Okay", null));

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
                Translation.Get(headerTranslation, "Error", this.header);
                Translation.Get(bodyTranslation, "This textbox is unable to display the input configuration.", this.body);

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

