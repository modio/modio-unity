using System;
using System.Collections.Generic;
using System.Linq;
using ModIO.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ModIOBrowser.Implementation
{

    public class KeyInput5DigitsUi : SelfInstancingMonoSingleton<KeyInput5DigitsUi>
    {
        public KeyInput5Digits keyInput5Digits;
        public List<TMP_Text> texts = new List<TMP_Text>();
        public List<GameObject> indicators = new List<GameObject>();
        public TMP_Text instructionText;

        int MaxDigits => texts.Count;
        Action onCancel;
        Action<string> onContinue;

        internal Translation AuthenticationPanelInfoTextTranslation = null;

        protected override void Awake()
        {
            base.Awake();
            keyInput5Digits.Setup();
        }

        /// <summary>
        /// Sets up the KeyInput5DigitsUi for use.
        /// </summary>
        /// <param name="onContinue">Triggers when the user continues with a code.</param>
        /// <param name="email">The email the user has input</param>
        /// <param name="onCancel">Triggers when the cancel button is called.</param>
        public void Open(Action<string> onContinue, string email, Action onCancel)
        {
            this.onCancel = onCancel;
            this.onContinue = onContinue;

            gameObject.SetActive(true);
            EventSystem.current.SetSelectedGameObject(null);
            
            keyInput5Digits.NewSession(MaxDigits, Render, Continue);
            SelectionManager.Instance.SelectView(UiViews.Input5Digits);

            Translation.Get(AuthenticationPanelInfoTextTranslation,
                "Please check your email {email} for your 5 digit code to verify it below.",
                instructionText,
                email);
        }

        #region UI frontend
        public void SetIndex(int i)
        {
            keyInput5Digits.SetIndex(i);
            OnIndexChange(i);
        }

        public void ContinueButton()
        {
            Continue(keyInput5Digits.GetValues());
        }

        public void CancelButton()
        {
            Close();
            onCancel?.Invoke();
        }
        #endregion

        void Close()
        {
            gameObject.SetActive(false);
            keyInput5Digits.EndSession();
        }

        void Continue(string s)
        {            
            Close();
            onContinue?.Invoke(s);
        }

        void Render(string renderString)
        {            
            texts.ForEach(t => t.text = "");
            foreach(var i in Enumerable.Range(0, Mathf.Min(MaxDigits, renderString.Length)))
                texts[i].text = renderString[i].ToString();

            OnIndexChange(keyInput5Digits.index);
        }


        void OnIndexChange(int i)
        {
            indicators.ForEach(x => x.gameObject.SetActive(false));
            indicators[i].gameObject.SetActive(true);
        }
    }
}
