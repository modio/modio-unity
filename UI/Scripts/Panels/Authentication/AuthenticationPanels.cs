using System;
using System.Collections;
using ModIO;
using ModIO.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    public class AuthenticationPanels : SimpleMonoSingleton<AuthenticationPanels>
    {

        internal Translation BrowserFeaturedSubscribeTranslation = null;
        internal Translation BrowserAuthenticationPanelTitle = null;
        internal Translation BrowserAuthenticationPanelInfo = null;
        internal Translation AuthenticationPanelBackButtonTextTranslation = null;
        internal Translation AuthenticationPanelInfoTextTranslation = null;
        internal Translation AuthenticationPanelTitleTextTranslation = null;
        
        [Header("Authentication Panel")]
        [SerializeField] public GameObject AuthenticationPanel;
        [SerializeField] public GameObject AuthenticationMainPanel;
        [SerializeField] public GameObject AuthenticationPanelWaitingForResponseAnimation;
        [SerializeField] public GameObject AuthenticationPanelEnterEmail;
        [SerializeField] public GameObject AuthenticationPanelLogo;
        [SerializeField] public TMP_InputField AuthenticationPanelEmailField;
        [SerializeField] public GameObject AuthenticationPanelEnterCode;
        [SerializeField] public TMP_InputField[] AuthenticationPanelCodeFields;
        [SerializeField] public TMP_InputField AuthenticationPanelHiddenInputField;
        [SerializeField] public Button AuthenticationPanelConnectViaSteamButton;
        [SerializeField] public Button AuthenticationPanelConnectViaXboxButton;
        [SerializeField] public Button AuthenticationPanelConnectViaSwitchButton;
        [SerializeField] public Button AuthenticationPanelConnectViaPlayStationButton;
        [SerializeField] public Button AuthenticationPanelConnectViaEmailButton;
        [SerializeField] public Button AuthenticationPanelBackButton;
        [SerializeField] public TMP_Text AuthenticationPanelBackButtonText;
        [SerializeField] public Button AuthenticationPanelAgreeButton;
        [SerializeField] public Button AuthenticationPanelSendCodeButton;
        [SerializeField] public Button AuthenticationPanelSubmitButton;
        [SerializeField] public Button AuthenticationPanelCompletedButton;
        [SerializeField] public Button AuthenticationPanelLogoutButton;
        [SerializeField] public Button AuthenticationPanelTOSButton;
        [SerializeField] public Button AuthenticationPanelPrivacyPolicyButton;
        [SerializeField] public Button AuthenticationPanelCancelButton;
        [SerializeField] public GameObject AuthenticationPanelTermsOfUseLinks;
        [SerializeField] public TMP_Text AuthenticationPanelTitleText;
        [SerializeField] public TMP_Text AuthenticationPanelInfoText;

        Action authenticationMethodAfterAgreeingToTheTOS;

        public Authentication auth;

        //Used by buttons
        public void Close()
        {
            AuthenticationPanel.SetActive(false);
            SelectionManager.Instance.SelectPreviousView();
        }

        void Logout()
        {
            if(ModIOUnity.LogOutCurrentUser().Succeeded())
            {
                Avatar.Instance.Avatar_Main.gameObject.SetActive(false);
                Authentication.Instance.IsAuthenticated = false;
                Close();
            }
            else
            {
                // TODO inform the user if this failed (Which really shouldn't ever fail)
            }
        }

        public void Open()
        {
            HideAllPanels();

            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);
            SelectionManager.Instance.SelectView(UiViews.AuthPanel);

            if(!SkippedIntoEmailConnectionPanel())
            {                
                OpenConnectionTypePanel();
            }            
        }

        bool SkippedIntoEmailConnectionPanel()
        {
            if(Authentication.getSteamAppTicket == null
                && Authentication.getXboxToken == null
                && Authentication.getSwitchToken == null
                && Authentication.getPlayStationAuthCode == null)
            {
                Authentication.Instance.GetTermsOfUse();
                authenticationMethodAfterAgreeingToTheTOS = OpenPanel_Email;
                
                return true;
            }

            return false;
        }

        void OpenConnectionTypePanel()
        {
            AuthenticationPanelTitleText.gameObject.SetActive(true);

            AuthenticationPanelLogo.SetActive(true);
            Translation.Get(BrowserAuthenticationPanelTitle, "Authentication", AuthenticationPanelTitleText);

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            Translation.Get(BrowserAuthenticationPanelInfo,
                "mod.io is a 3rd party utility that provides access to a mod workshop. Choose how you wish to be authenticated.",
                AuthenticationPanelInfoText);

            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(Close);

            AuthenticationPanelConnectViaEmailButton.gameObject.SetActive(true);
            AuthenticationPanelConnectViaEmailButton.onClick.RemoveAllListeners();
            AuthenticationPanelConnectViaEmailButton.onClick.AddListener(() =>
            {
                Authentication.Instance.GetTermsOfUse();
                authenticationMethodAfterAgreeingToTheTOS = OpenPanel_Email;
            });

            // Selection
            InputNavigation.Instance.Select(AuthenticationPanelConnectViaEmailButton);
            
            //-----------------------------------------------------------------------------------//
            //                            THIRD PARTY AUTHENTICATION                             //
            //-----------------------------------------------------------------------------------//
            Selectable thirdPartyOptionSelectable = null;
            if(Authentication.getSteamAppTicket != null)
            {
                AuthenticationPanelConnectViaSteamButton.gameObject.SetActive(true);
                AuthenticationPanelConnectViaSteamButton.onClick.RemoveAllListeners();
                AuthenticationPanelConnectViaSteamButton.onClick.AddListener(() =>
                {
                    Authentication.Instance.GetTermsOfUse();
                    authenticationMethodAfterAgreeingToTheTOS = Authentication.Instance.SubmitSteamAuthenticationRequest;
                });
                thirdPartyOptionSelectable = AuthenticationPanelConnectViaSteamButton;
                
                InputNavigation.Instance.Select(AuthenticationPanelConnectViaSteamButton);
            }
            else if(Authentication.getXboxToken != null)
            {
                AuthenticationPanelConnectViaXboxButton.gameObject.SetActive(true);
                AuthenticationPanelConnectViaXboxButton.onClick.RemoveAllListeners();
                AuthenticationPanelConnectViaXboxButton.onClick.AddListener(() =>
                {
                    Authentication.Instance.GetTermsOfUse();
                    authenticationMethodAfterAgreeingToTheTOS = Authentication.Instance.SubmitXboxAuthenticationRequest;
                });
                thirdPartyOptionSelectable = AuthenticationPanelConnectViaXboxButton;
                
                InputNavigation.Instance.Select(AuthenticationPanelConnectViaXboxButton);
            }
            else if(Authentication.getSwitchToken != null)
            {
                AuthenticationPanelConnectViaSwitchButton.gameObject.SetActive(true);
                AuthenticationPanelConnectViaSwitchButton.onClick.RemoveAllListeners();
                AuthenticationPanelConnectViaSwitchButton.onClick.AddListener(() =>
                {
                    Authentication.Instance.GetTermsOfUse();
                    authenticationMethodAfterAgreeingToTheTOS = Authentication.Instance.SubmitSwitchAuthenticationRequest;
                });
                thirdPartyOptionSelectable = AuthenticationPanelConnectViaSwitchButton;
                
                InputNavigation.Instance.Select(AuthenticationPanelConnectViaSwitchButton);
            }
            else if(Authentication.getPlayStationAuthCode != null)
            {
                AuthenticationPanelConnectViaPlayStationButton.gameObject.SetActive(true);
                AuthenticationPanelConnectViaPlayStationButton.onClick.RemoveAllListeners();
                AuthenticationPanelConnectViaPlayStationButton.onClick.AddListener(() =>
                {
                    Authentication.Instance.GetTermsOfUse();
                    authenticationMethodAfterAgreeingToTheTOS = Authentication.Instance.SubmitPlayStationAuthenticationRequest;
                });
                thirdPartyOptionSelectable = AuthenticationPanelConnectViaPlayStationButton;
                
                InputNavigation.Instance.Select(AuthenticationPanelConnectViaPlayStationButton);
            }

            //-----------------------------------------------------------------------------------//
            //                            EXPLICIT BUTTON NAVIGATION                             //
            //-----------------------------------------------------------------------------------//

            // Back button
            AuthenticationPanelBackButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnRight = thirdPartyOptionSelectable == null
                    ? AuthenticationPanelConnectViaEmailButton : thirdPartyOptionSelectable
            };

            // Third party Auth button (This button may or may not be present)
            if(thirdPartyOptionSelectable != null)
            {
                thirdPartyOptionSelectable.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = AuthenticationPanelBackButton,
                    selectOnRight = AuthenticationPanelConnectViaEmailButton
                };
            }

            // email auth button
            AuthenticationPanelConnectViaEmailButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = thirdPartyOptionSelectable == null
                    ? AuthenticationPanelBackButton : thirdPartyOptionSelectable
            };
            
        }

        void HideAllPanels()
        {
            AuthenticationPanelTitleText.gameObject.SetActive(false);
            AuthenticationPanelInfoText.gameObject.SetActive(false);
            TextAlignmentOptions alignment = AuthenticationPanelInfoText.alignment;
            alignment = TextAlignmentOptions.Left;
            AuthenticationPanelInfoText.alignment = alignment;
            Translation.Get(AuthenticationPanelBackButtonTextTranslation, "Back", AuthenticationPanelBackButtonText);
            AuthenticationPanelEnterCode.SetActive(false);
            AuthenticationPanelEnterEmail.SetActive(false);
            AuthenticationPanelTermsOfUseLinks.SetActive(false);
            AuthenticationPanelWaitingForResponseAnimation.SetActive(false);
            AuthenticationPanelAgreeButton.gameObject.SetActive(false);
            AuthenticationPanelBackButton.gameObject.SetActive(false);
            AuthenticationPanelSubmitButton.gameObject.SetActive(false);
            AuthenticationPanelSendCodeButton.gameObject.SetActive(false);
            AuthenticationPanelConnectViaEmailButton.gameObject.SetActive(false);
            AuthenticationPanelConnectViaSteamButton.gameObject.SetActive(false);
            AuthenticationPanelConnectViaXboxButton.gameObject.SetActive(false);
            AuthenticationPanelConnectViaSwitchButton.gameObject.SetActive(false);
            AuthenticationPanelConnectViaPlayStationButton.gameObject.SetActive(false);
            AuthenticationPanelCompletedButton.gameObject.SetActive(false);
            AuthenticationPanelLogoutButton.gameObject.SetActive(false);
            AuthenticationPanelWaitingForResponseAnimation.SetActive(false);
            AuthenticationPanelLogo.SetActive(false);
            AuthenticationPanelCancelButton.gameObject.SetActive(false);
        }

        //Used by buttons
        public void HyperLinkToTOS()
        {
            if(string.IsNullOrWhiteSpace(Authentication.Instance.termsOfUseURL) || Authentication.Instance.LastReceivedTermsOfUse.links == null)
            {
                WebBrowser.OpenWebPage("https://mod.io/terms");
            }
            else
            {
                WebBrowser.OpenWebPage(Authentication.Instance.termsOfUseURL);
            }
        }

        //Used by buttons
        public void HyperLinkToPrivacyPolicy()
        {
            if(string.IsNullOrWhiteSpace(Authentication.Instance.privacyPolicyURL) || Authentication.Instance.LastReceivedTermsOfUse.links == null)
            {
                WebBrowser.OpenWebPage("https://mod.io/privacy");
            }
            else
            {
                WebBrowser.OpenWebPage(Authentication.Instance.privacyPolicyURL);
            }
        }

        public void OpenPanel_Waiting()
        {
            HideAllPanels();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(false);
            AuthenticationPanelWaitingForResponseAnimation.SetActive(true);

            AuthenticationPanelInfoText.gameObject.SetActive(true);

            Translation.Get(AuthenticationPanelInfoTextTranslation, "Waiting for response...", AuthenticationPanelInfoText);
            TextAlignmentOptions alignment = AuthenticationPanelInfoText.alignment;
            alignment = TextAlignmentOptions.Center;
            AuthenticationPanelInfoText.alignment = alignment;

            //AuthenticationWaitingPanelBackButton.Select();
        }

        public void OpenPanel_Logout(Action onBack = null)
        {
            HideAllPanels();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);

            AuthenticationPanelTitleText.gameObject.SetActive(true);

            Translation.Get(AuthenticationPanelTitleTextTranslation, "Are you sure you'd like to log out?", AuthenticationPanelTitleText);
            AuthenticationPanelLogo.SetActive(true);

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            Translation.Get(AuthenticationPanelInfoTextTranslation, "LogOutMessage", AuthenticationPanelInfoText);

            AuthenticationPanelBackButton.gameObject.SetActive(true);
            Translation.Get(AuthenticationPanelBackButtonTextTranslation, "Cancel", AuthenticationPanelBackButtonText);

            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(delegate
            {
                Close();
                onBack?.Invoke();
            });

            AuthenticationPanelBackButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnRight = AuthenticationPanelLogoutButton
            };

            AuthenticationPanelLogoutButton.gameObject.SetActive(true);
            AuthenticationPanelLogoutButton.onClick.RemoveAllListeners();
            AuthenticationPanelLogoutButton.onClick.AddListener(Logout);

            AuthenticationPanelLogoutButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = AuthenticationPanelBackButton
            };

            SelectionManager.Instance.SelectView(UiViews.AuthPanel_LogOut);

            LayoutRebuilder.ForceRebuildLayoutImmediate(AuthenticationPanelInfoText.transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(AuthenticationPanel.transform as RectTransform);
        }
        
        public void OpenPanel_Problem(string problemTranslationKey = null, string titleTranslationKey = null, Action onBack = null)
        {
            titleTranslationKey = titleTranslationKey ?? "Something went wrong!";
            problemTranslationKey = problemTranslationKey ?? "We were unable to connect to the mod.io server. Check you have a stable internet connection and try again.";

            HideAllPanels();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);

            AuthenticationPanelTitleText.gameObject.SetActive(true);
            Translation.Get(AuthenticationPanelTitleTextTranslation, titleTranslationKey, AuthenticationPanelTitleText);

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            Translation.Get(AuthenticationPanelInfoTextTranslation, problemTranslationKey, AuthenticationPanelInfoText);

            AuthenticationPanelCancelButton.gameObject.SetActive(true);
            
            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            if(onBack == null)
            {
                onBack = Close;
            }
            AuthenticationPanelBackButton.onClick.AddListener(delegate { onBack(); });

            AuthenticationPanelCancelButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = AuthenticationPanelBackButton
            };
            AuthenticationPanelBackButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnRight = AuthenticationPanelCancelButton
            };

            InputNavigation.Instance.Select(AuthenticationPanelBackButton);
        }

        public void OpenPanel_TermsOfUse()
            => OpenPanel_TermsOfUse(null);

        public void OpenPanel_TermsOfUse(string TOS = null)
        {
            HideAllPanels();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);
            AuthenticationPanelTermsOfUseLinks.SetActive(true);

            AuthenticationPanelTitleText.gameObject.SetActive(true);

            Translation.Get(AuthenticationPanelTitleTextTranslation, "Terms of use", AuthenticationPanelTitleText);

            AuthenticationPanelInfoText.gameObject.SetActive(true);

            if(TOS != null)
            {
                AuthenticationPanelInfoText.text = TOS;
            }

            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(Close);

            AuthenticationPanelBackButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnRight = AuthenticationPanelAgreeButton,
                selectOnUp = AuthenticationPanelTOSButton
            };

            AuthenticationPanelAgreeButton.gameObject.SetActive(true);
            AuthenticationPanelAgreeButton.onClick.RemoveAllListeners();
            // TODO go to either steam or email auth next
            AuthenticationPanelAgreeButton.onClick.AddListener(delegate { authenticationMethodAfterAgreeingToTheTOS(); });

            AuthenticationPanelAgreeButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = AuthenticationPanelBackButton,
                selectOnUp = AuthenticationPanelPrivacyPolicyButton
            };

            InputNavigation.Instance.Select(AuthenticationPanelAgreeButton);
        }

        public void OpenPanel_Email()
        {
            HideAllPanels();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);

            AuthenticationPanelEnterEmail.SetActive(true);
            AuthenticationPanelEmailField.text = "";

            AuthenticationPanelEmailField.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnDown = AuthenticationPanelSendCodeButton
            };

            AuthenticationPanelTitleText.gameObject.SetActive(true);
            Translation.Get(AuthenticationPanelTitleTextTranslation, "Email authentication", AuthenticationPanelTitleText);

            AuthenticationPanelInfoText.gameObject.SetActive(false);

            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(OpenPanel_TermsOfUse);

            AuthenticationPanelBackButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnRight = AuthenticationPanelSendCodeButton,
                selectOnUp = AuthenticationPanelEmailField
            };

            AuthenticationPanelSendCodeButton.gameObject.SetActive(true);
            AuthenticationPanelSendCodeButton.onClick.RemoveAllListeners();
            AuthenticationPanelSendCodeButton.onClick.AddListener(Authentication.Instance.SendEmail);

            AuthenticationPanelSendCodeButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = AuthenticationPanelBackButton,
                selectOnUp = AuthenticationPanelEmailField,
                selectOnRight = AuthenticationPanelCancelButton
            };

            AuthenticationPanelCancelButton.gameObject.SetActive(true);
            AuthenticationPanelCancelButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnLeft = AuthenticationPanelSendCodeButton,
                selectOnUp = AuthenticationPanelEmailField
            };

            InputNavigation.Instance.Select(AuthenticationPanelEmailField);
        }

        public void OpenPanel_Code()
        {
            HideAllPanels();

            var newKeyInput = false;

#if UNITY_STANDALONE || UNITY_EDITOR
            newKeyInput = true;
            //just making sure the code is readable & compile-able
#endif
            if(newKeyInput)
            {
                KeyInput5DigitsUi.Instance.Open(
                    code => ModIOUnity.SubmitEmailSecurityCode(code, Authentication.Instance.CodeSubmitted),
                    AuthenticationPanelEmailField.text,
                    OpenPanel_Email);
            }
            else
            {
                OpenPanel_CodeSentNoticeForVirtualKeyboardUser();
            }
        }

        /// <summary>
        /// This brings up a notice telling the user their code has been sent to the provided email.
        /// Pressing 'Continue' will then close the panel and bring up the Virtual Keyboard to
        /// input the 5 digit code.
        /// </summary>
        void OpenPanel_CodeSentNoticeForVirtualKeyboardUser()
        {
            HideAllPanels();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);

            AuthenticationPanelTitleText.gameObject.SetActive(true);
            Translation.Get(AuthenticationPanelTitleTextTranslation, "Code sent", AuthenticationPanelTitleText);

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            Translation.Get(AuthenticationPanelInfoTextTranslation, "Please check your email {email} for your 5 digit code to verify it below.", AuthenticationPanelInfoText, AuthenticationPanelEmailField.text);

            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            
            Translation.Get(AuthenticationPanelBackButtonTextTranslation, "Enter code", AuthenticationPanelBackButtonText);
            
            AuthenticationPanelBackButton.onClick.AddListener(SelectHiddenInputFieldForVirtualKeyboardUser);

            AuthenticationPanelBackButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit
            };

            InputNavigation.Instance.Select(AuthenticationPanelBackButton);
        }

        /// <summary>
        /// This selects the hidden input field which should then automatically open the virtual
        /// keyboard. No other panel will be visible when this method is invoked until the input
        /// field has been edited. (If the virtual keyboard doesnt show up, pressing any key should
        /// continue the dialog)
        /// </summary>
        void SelectHiddenInputFieldForVirtualKeyboardUser()
        {
            HideAllPanels();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(false);
            AuthenticationPanelHiddenInputField.Select();
        }

        /// <summary>
        /// This is called when the hidden input field (for controller use only) has been edited.
        /// The hidden input field is used solely for inputting the 5 digit code on controllers.
        /// </summary>
        public void OnEndEditHiddenInput()
        {
            OpenPanel_Waiting();
            ModIOUnity.SubmitEmailSecurityCode(AuthenticationPanelHiddenInputField.text, Authentication.Instance.CodeSubmitted);
        }
        
        // void OpenPanel_Code_Old()
        // {
        //     
        //     AuthenticationPanel.SetActive(true);
        //     AuthenticationMainPanel.SetActive(true);
        //     AuthenticationPanelEnterCode.SetActive(true);
        //     for(int i = 0; i < AuthenticationPanelCodeFields.Length; i++)
        //     {
        //         if(i == 0)
        //         {
        //             InputNavigation.Instance.Select(AuthenticationPanelCodeFields[i]);
        //         }
        //         AuthenticationPanelCodeFields[i].onValueChanged.RemoveAllListeners();
        //         AuthenticationPanelCodeFields[i].text = "";
        //         if(i == AuthenticationPanelCodeFields.Length - 1)
        //         {
        //             int previous = i == 0 ? 0 : i - 1;
        //             AuthenticationPanelCodeFields[i].onValueChanged.AddListener((fieldText) =>
        //                 CodeDigitFieldOnValueChangeBehaviour(AuthenticationPanelCodeFields[previous], AuthenticationPanelSubmitButton, fieldText));
        //         }
        //         else
        //         {
        //             int next = i + 1;
        //             int previous = i == 0 ? 0 : i - 1;
        //             AuthenticationPanelCodeFields[i].onValueChanged.AddListener((fieldText) =>
        //                 CodeDigitFieldOnValueChangeBehaviour(AuthenticationPanelCodeFields[previous], AuthenticationPanelCodeFields[next], fieldText));
        //         }
        //     }
        //
        //     AuthenticationPanelTitleText.gameObject.SetActive(true);
        //     Translation.Get(AuthenticationPanelTitleTextTranslation, "Email authentication", AuthenticationPanelTitleText);
        //
        //     AuthenticationPanelInfoText.gameObject.SetActive(true);
        //     Translation.Get(AuthenticationPanelInfoTextTranslation,
        //         "Please check your email {email} for your 5 digit code to verify it below.",
        //         AuthenticationPanelInfoText,
        //         AuthenticationPanelEmailField.text);
        //
        //     AuthenticationPanelBackButton.gameObject.SetActive(true);
        //     AuthenticationPanelBackButton.onClick.RemoveAllListeners();
        //     AuthenticationPanelBackButton.onClick.AddListener(OpenPanel_Email);
        //
        //     AuthenticationPanelBackButton.navigation = new Navigation
        //     {
        //         mode = Navigation.Mode.Explicit,
        //         selectOnRight = AuthenticationPanelSubmitButton
        //     };
        //
        //     AuthenticationPanelSubmitButton.gameObject.SetActive(true);
        //     AuthenticationPanelSubmitButton.onClick.RemoveAllListeners();
        //     AuthenticationPanelSubmitButton.onClick.AddListener(Authentication.Instance.SubmitAuthenticationCode);
        //
        //     AuthenticationPanelSubmitButton.navigation = new Navigation
        //     {
        //         mode = Navigation.Mode.Explicit,
        //         selectOnLeft = AuthenticationPanelBackButton
        //     };
        // }

        void CodeDigitFieldOnValueChangeBehaviour(Selectable previous, Selectable next, string field)
        {
            // This would be the result of using CTRL + V on the first field
            if(field.Length == 5)
            {
                // Iterate over the field string and set each input field's text to each character
                for(int i = 0; i < AuthenticationPanelCodeFields.Length && i < field.Length; i++)
                {
                    AuthenticationPanelCodeFields[i].SetTextWithoutNotify(field[i].ToString());
                }
                // Set the final selection change on the next frame
                StartCoroutine(NextFrameSelectionChange(AuthenticationPanelSubmitButton));
            }
            // This is the block used for a regular single digit input into the field
            else if(field.Length < 2)
            {
                if(string.IsNullOrEmpty(field))
                {
                    InputNavigation.Instance.Select(previous);
                }
                else
                {
                    InputNavigation.Instance.Select(next);
                }
            }
        }

        /// <summary>
        /// This is used because of a strange Unity behaviour with Selectable.Select() and the way
        /// it is being used with the copy/paste support. If Select() is called, then ny other
        /// consecutive calls made on the same frame seem to fail, hence this coroutine.
        /// </summary>
        /// <seealso cref="CodeDigitFieldOnValueChangeBehaviour"/>
        IEnumerator NextFrameSelectionChange(Selectable selectable)
        {
            yield return null;
            InputNavigation.Instance.Select(selectable);
        }

        public void OpenPanel_Complete()
        {
            Authentication.Instance.IsAuthenticated = true;

            HideAllPanels();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);

            AuthenticationPanelTitleText.gameObject.SetActive(true);
            Translation.Get(AuthenticationPanelTitleTextTranslation, "Authentication completed", AuthenticationPanelTitleText);

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            Translation.Get(AuthenticationPanelInfoTextTranslation, "You are now connected to the mod.io browser. You can now subscribe to mods to use in your game and track them in your Collection.", AuthenticationPanelInfoText);

            AuthenticationPanelCompletedButton.gameObject.SetActive(true);

            AuthenticationPanelCompletedButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
            };

            // Update the user avatar display
            Avatar.Instance.SetupUser();

            // Refresh list items in home view (such as subscribed badge etc)
            Home.Instance.RefreshModListItems();

            //SelectionManager.Instance.SelectView(UiViews.AuthPanel_Complete);
            InputNavigation.Instance.Select(AuthenticationPanelCompletedButton);
        }
    }
}
