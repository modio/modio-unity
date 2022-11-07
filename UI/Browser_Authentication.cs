using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModIO;
using ModIOBrowser.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    public partial class Browser
    {
        [Header("Authentication Panel")]
        [SerializeField] GameObject AuthenticationPanel;
        [SerializeField] GameObject AuthenticationMainPanel;
        [SerializeField] GameObject AuthenticationPanelWaitingForResponseAnimation;
        [SerializeField] GameObject AuthenticationPanelEnterEmail;
        [SerializeField] GameObject AuthenticationPanelLogo;
        [SerializeField] TMP_InputField AuthenticationPanelEmailField;
        [SerializeField] GameObject AuthenticationPanelEnterCode;
        [SerializeField] TMP_InputField[] AuthenticationPanelCodeFields;
        [SerializeField] Button AuthenticationPanelConnectViaSteamButton;
        [SerializeField] Button AuthenticationPanelConnectViaXboxButton;
        [SerializeField] Button AuthenticationPanelConnectViaSwitchButton;
        [SerializeField] Button AuthenticationPanelConnectViaEmailButton;
        [SerializeField] Button AuthenticationPanelBackButton;
        [SerializeField] TMP_Text AuthenticationPanelBackButtonText;
        [SerializeField] Button AuthenticationPanelAgreeButton;
        [SerializeField] Button AuthenticationPanelSendCodeButton;
        [SerializeField] Button AuthenticationPanelSubmitButton;
        [SerializeField] Button AuthenticationPanelCompletedButton;
        [SerializeField] Button AuthenticationPanelLogoutButton;
        [SerializeField] Button AuthenticationPanelTOSButton;
        [SerializeField] Button AuthenticationPanelPrivacyPolicyButton;
        [SerializeField] GameObject AuthenticationPanelTermsOfUseLinks;
        [SerializeField] TMP_Text AuthenticationPanelTitleText;
        [SerializeField] TMP_Text AuthenticationPanelInfoText;
        [SerializeField] Image Avatar_Main;
        [SerializeField] Image AvatarDownloadBar;

        [Header("Platform Avatar Icons")]
        [SerializeField] Image PlatformIcon_Main;
        [SerializeField] Image PlatformIcon_DownloadQueue;
        [SerializeField] Sprite SteamAvatar;
        [SerializeField] Sprite XboxAvatar;

        // Authentication Delegates
        Action authenticationMethodAfterAgreeingToTheTOS;
        static string optionalThirdPartyEmailAddressUsedForAuthentication;
        static string optionalSteamAppTicket;
        static string optionalXboxToken;
        static string optionalSwitchToken;
        
        UserProfile currentUserProfile;
        TermsOfUse LastReceivedTermsOfUse;
        string privacyPolicyURL;
        string termsOfUseURL;

        UserPortal currentAuthenticationPortal = UserPortal.None;

#region Download bar
        internal void UpdateAvatarDownloadProgressBar(ProgressHandle handle)
        {
            AvatarDownloadBar.fillAmount = handle == null ? 0f : handle.Progress;
        }
#endregion

#region Managing Panels
        public void CloseAuthenticationPanel()
        {
            AuthenticationPanel.SetActive(false);
            SelectionManager.Instance.SelectPreviousView();
        }

        public void UpdateBrowserModListItemDisplay()
        {
            List<SubscribedMod> subbedMods = ModIOUnity.GetSubscribedMods(out var result).ToList();

            if(!result.Succeeded())
            {
                return;
            }

            BrowserModListItem.listItems.Where(x => x.Value.isActiveAndEnabled)
                .ToList()
                .ForEach(x =>
                {
                    if(subbedMods.Any(mod => mod.modProfile.Equals(x.Value.profile)))
                    {
                        x.Value.Setup(x.Value.profile);
                    }
                });
        }

        public void Logout()
        {
            if(ModIOUnity.LogOutCurrentUser().Succeeded())
            {
                Avatar_Main.gameObject.SetActive(false);
                isAuthenticated = false;
            }
            else
            {
                // TODO inform the user if this failed (Which really shouldn't ever fail)
            }
        }

        public void OpenAuthenticationPanel()
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);
            

            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Authentication";
            
            AuthenticationPanelLogo.SetActive(true);

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = "mod.io is a 3rd party utility that provides access to a mod workshop. Choose how you wish to be authenticated.";

            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(CloseAuthenticationPanel);

            AuthenticationPanelConnectViaEmailButton.gameObject.SetActive(true);
            AuthenticationPanelConnectViaEmailButton.onClick.RemoveAllListeners();
            AuthenticationPanelConnectViaEmailButton.onClick.AddListener(() =>
            {
                GetTermsOfUse();
                authenticationMethodAfterAgreeingToTheTOS = OpenAuthenticationPanel_Email;
            });
            
            //-----------------------------------------------------------------------------------//
            //                            THIRD PARTY AUTHENTICATION                             //
            //-----------------------------------------------------------------------------------//
            Selectable thirdPartyOptionSelectable = null;
            if(optionalSteamAppTicket != null)
            {
                AuthenticationPanelConnectViaSteamButton.gameObject.SetActive(true);
                AuthenticationPanelConnectViaSteamButton.onClick.RemoveAllListeners();
                AuthenticationPanelConnectViaSteamButton.onClick.AddListener(() =>
                {
                    GetTermsOfUse();
                    authenticationMethodAfterAgreeingToTheTOS = SubmitSteamAuthenticationRequest;
                });
                thirdPartyOptionSelectable = AuthenticationPanelConnectViaSteamButton;
            }
            else if(optionalXboxToken != null)
            {
                AuthenticationPanelConnectViaXboxButton.gameObject.SetActive(true);
                AuthenticationPanelConnectViaXboxButton.onClick.RemoveAllListeners();
                AuthenticationPanelConnectViaXboxButton.onClick.AddListener(() =>
                {
                    GetTermsOfUse();
                    authenticationMethodAfterAgreeingToTheTOS = SubmitXboxAuthenticationRequest;
                });
                thirdPartyOptionSelectable = AuthenticationPanelConnectViaXboxButton;
            }
            else if(optionalSwitchToken != null)
            {
                AuthenticationPanelConnectViaSwitchButton.gameObject.SetActive(true);
                AuthenticationPanelConnectViaSwitchButton.onClick.RemoveAllListeners();
                AuthenticationPanelConnectViaSwitchButton.onClick.AddListener(() =>
                {
                    GetTermsOfUse();
                    authenticationMethodAfterAgreeingToTheTOS = SubmitSwitchAuthenticationRequest;
                });
                thirdPartyOptionSelectable = AuthenticationPanelConnectViaSwitchButton;
            }
            
            //-----------------------------------------------------------------------------------//
            //                            EXPLICIT BUTTON NAVIGATION                             //
            //-----------------------------------------------------------------------------------//

            // Back button
            AuthenticationPanelBackButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnRight = thirdPartyOptionSelectable == null 
                    ? AuthenticationPanelConnectViaEmailButton : thirdPartyOptionSelectable
            };
            
            // Third party Auth button (This button may or may not be present)
            if(thirdPartyOptionSelectable != null)
            {
                thirdPartyOptionSelectable.navigation = new Navigation{
                    mode = Navigation.Mode.Explicit,
                    selectOnLeft = AuthenticationPanelBackButton,
                    selectOnRight = AuthenticationPanelConnectViaEmailButton
                };
            }
            
            // email auth button
            AuthenticationPanelConnectViaEmailButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnLeft = thirdPartyOptionSelectable == null 
                    ? AuthenticationPanelBackButton : thirdPartyOptionSelectable
            };

            SelectionManager.Instance.SelectView(UiViews.AuthPanel);
        }

        internal void HideAuthenticationPanelObjects()
        {
            AuthenticationPanelTitleText.gameObject.SetActive(false);
            AuthenticationPanelInfoText.gameObject.SetActive(false);
            TextAlignmentOptions alignment = AuthenticationPanelInfoText.alignment;
            alignment = TextAlignmentOptions.Left;
            AuthenticationPanelInfoText.alignment = alignment;
            AuthenticationPanelBackButtonText.text = "Back";
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
            AuthenticationPanelCompletedButton.gameObject.SetActive(false);
            AuthenticationPanelLogoutButton.gameObject.SetActive(false);
            AuthenticationPanelWaitingForResponseAnimation.SetActive(false);
            AuthenticationPanelLogo.SetActive(false);
        }

        internal void HyperLinkToTOS()
        {
            if(string.IsNullOrWhiteSpace(termsOfUseURL) || LastReceivedTermsOfUse.links == null)
            {
                Application.OpenURL("https://mod.io/terms");
            }
            else
            {
                Application.OpenURL(termsOfUseURL);
            }
        }

        internal void HyperLinkToPrivacyPolicy()
        {
            if(string.IsNullOrWhiteSpace(privacyPolicyURL) || LastReceivedTermsOfUse.links == null)
            {
                Application.OpenURL("https://mod.io/privacy");
            }
            else
            {
                Application.OpenURL(privacyPolicyURL);
            }
        }

        internal void OpenAuthenticationPanel_Waiting()
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(false);
            AuthenticationPanelWaitingForResponseAnimation.SetActive(true);

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = "Waiting for response...";
            TextAlignmentOptions alignment = AuthenticationPanelInfoText.alignment;
            alignment = TextAlignmentOptions.Center;
            AuthenticationPanelInfoText.alignment = alignment;
            
            //AuthenticationWaitingPanelBackButton.Select();
        }
        
        public void OpenAuthenticationPanel_Logout(Action onBack = null)
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);

            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Are you sure you'd like to log out?";
            
            AuthenticationPanelLogo.SetActive(true);

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = "This will log you out of your mod.io account."
                                               + " You can still browse the mods but you will "
                                               + "need to log back in to subscribe to a mod. Any"
                                               + " ongoing downloads/installations will also be"
                                               + " stopped.\n\nDo you wish to continue?";

            AuthenticationPanelBackButtonText.text = "Cancel";
            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(delegate
            {
                CloseAuthenticationPanel();
                onBack?.Invoke();
            });
            
            AuthenticationPanelBackButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnRight = AuthenticationPanelLogoutButton
            };

            AuthenticationPanelLogoutButton.gameObject.SetActive(true);
            AuthenticationPanelLogoutButton.onClick.RemoveAllListeners();
            AuthenticationPanelLogoutButton.onClick.AddListener(Logout);
            
            AuthenticationPanelLogoutButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnLeft = AuthenticationPanelBackButton
            };

            SelectionManager.Instance.SelectView(UiViews.AuthPanel_LogOut);
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(AuthenticationPanelInfoText.transform as RectTransform);
            LayoutRebuilder.ForceRebuildLayoutImmediate(AuthenticationPanel.transform as RectTransform);
        }

        internal void OpenAuthenticationPanel_Problem(string problem = null, string title = null, Action onBack = null)
        {
            title = title ?? "Something went wrong!";
            problem = problem ?? "We were unable to connect to the mod.io server. Check you have a stable internet connection and try again.";
            
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);
            
            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = title;

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = problem;
            
            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            if(onBack == null)
            {
                onBack = CloseAuthenticationPanel;
            }
            AuthenticationPanelBackButton.onClick.AddListener(delegate { onBack();});
            
            AuthenticationPanelBackButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
            };

            SelectSelectable(AuthenticationPanelBackButton);
        }
        
        internal void OpenAuthenticationPanel_TermsOfUse(string TOS)
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);
            AuthenticationPanelTermsOfUseLinks.SetActive(true);
            
            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Terms of use";

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = TOS;
            
            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(CloseAuthenticationPanel);
            
            AuthenticationPanelBackButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnRight = AuthenticationPanelAgreeButton,
                selectOnUp = AuthenticationPanelTOSButton
            };

            AuthenticationPanelAgreeButton.gameObject.SetActive(true);
            AuthenticationPanelAgreeButton.onClick.RemoveAllListeners();
            // TODO go to either steam or email auth next
            AuthenticationPanelAgreeButton.onClick.AddListener(delegate { authenticationMethodAfterAgreeingToTheTOS(); });
            
            AuthenticationPanelAgreeButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnLeft = AuthenticationPanelBackButton,
                selectOnUp = AuthenticationPanelPrivacyPolicyButton
            };

            SelectSelectable(AuthenticationPanelAgreeButton);
        }
        
        internal void OpenAuthenticationPanel_Email()
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);
            
            AuthenticationPanelEnterEmail.SetActive(true);
            AuthenticationPanelEmailField.text = "";
            
            AuthenticationPanelEmailField.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnDown = AuthenticationPanelSendCodeButton
            };
            
            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Email authentication";

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = LastReceivedTermsOfUse.termsOfUse;

            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(CloseAuthenticationPanel);
            
            AuthenticationPanelBackButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnRight = AuthenticationPanelSendCodeButton,
                selectOnUp = AuthenticationPanelEmailField
            };

            AuthenticationPanelSendCodeButton.gameObject.SetActive(true);
            AuthenticationPanelSendCodeButton.onClick.RemoveAllListeners();
            AuthenticationPanelSendCodeButton.onClick.AddListener(SendEmail);
            
            AuthenticationPanelSendCodeButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnLeft = AuthenticationPanelBackButton,
                selectOnUp = AuthenticationPanelEmailField
            };

            SelectSelectable(AuthenticationPanelEmailField);
        }

        internal void OpenAuthenticationPanel_Code()
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);
            AuthenticationPanelEnterCode.SetActive(true);
            for (int i = 0; i < AuthenticationPanelCodeFields.Length; i++)
            {
                if(i == 0)
                {
                    SelectSelectable(AuthenticationPanelCodeFields[i]);
                }
                AuthenticationPanelCodeFields[i].onValueChanged.RemoveAllListeners();
                AuthenticationPanelCodeFields[i].text = "";
                if (i == AuthenticationPanelCodeFields.Length - 1)
                {
                    int previous = i == 0 ? 0 : i - 1;
                    AuthenticationPanelCodeFields[i].onValueChanged.AddListener((fieldText) =>
                        CodeDigitFieldOnValueChangeBehaviour(AuthenticationPanelCodeFields[previous], AuthenticationPanelSubmitButton, fieldText));
                }
                else
                {
                    int next = i + 1;
                    int previous = i == 0 ? 0 : i - 1;
                    AuthenticationPanelCodeFields[i].onValueChanged.AddListener((fieldText) =>
                        CodeDigitFieldOnValueChangeBehaviour(AuthenticationPanelCodeFields[previous], AuthenticationPanelCodeFields[next], fieldText));
                }
            }

            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Email authentication";

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = "Once the email is received, enter the provided confidential code. It should be 5 characters long.";
            
            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(OpenAuthenticationPanel_Email);
            
            AuthenticationPanelBackButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnRight = AuthenticationPanelSubmitButton
            };

            AuthenticationPanelSubmitButton.gameObject.SetActive(true);
            AuthenticationPanelSubmitButton.onClick.RemoveAllListeners();
            AuthenticationPanelSubmitButton.onClick.AddListener(SubmitAuthenticationCode);
            
            AuthenticationPanelSubmitButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
                selectOnLeft = AuthenticationPanelBackButton
            };
        }

        internal void CodeDigitFieldOnValueChangeBehaviour(Selectable previous, Selectable next, string field)
        {
            // This would be the result of using CTRL + V on the first field
            if(field.Length == 5)
            {
                // Iterate over the field string and set each input field's text to each character
                for (int i = 0; i < AuthenticationPanelCodeFields.Length && i < field.Length; i++)
                {
                    AuthenticationPanelCodeFields[i].SetTextWithoutNotify(field[i].ToString());
                }
                // Set the final selection change on the next frame
                StartCoroutine(NextFrameSelectionChange(AuthenticationPanelSubmitButton));
            } 
            // This is the block used for a regular single digit input into the field
            else if (field.Length < 2)
            {
                if(string.IsNullOrEmpty(field))
                {
                    SelectSelectable(previous);
                }
                else
                {
                    SelectSelectable(next);
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
            SelectSelectable(selectable);
        }
        
        internal void OpenAuthenticationPanel_Complete()
        {
            isAuthenticated = true;
            
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationMainPanel.SetActive(true);
            
            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Authentication completed";

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = "You are now connected to the mod.io browser. You can now subscribe to mods to use in your game and track them in your Collection.";
            
            AuthenticationPanelCompletedButton.gameObject.SetActive(true);
            
            AuthenticationPanelCompletedButton.navigation = new Navigation{
                mode = Navigation.Mode.Explicit,
            };
            
            // Update the user avatar display
            SetupUserAvatar();

            //Hmmm
            SelectionManager.Instance.SelectView(UiViews.AuthPanel_Complete);
            SelectSelectable(AuthenticationPanelCompletedButton);
        }
        #endregion

#region Send Request
        internal void GetTermsOfUse()
        {
            OpenAuthenticationPanel_Waiting();
            ModIOUnity.GetTermsOfUse(ReceiveTermsOfUse);
        }
        internal void SendEmail()
        {
            OpenAuthenticationPanel_Waiting();
            ModIOUnity.RequestAuthenticationEmail(AuthenticationPanelEmailField.text, EmailSent);
        }

        internal void SubmitAuthenticationCode()
        {
            OpenAuthenticationPanel_Waiting();
            string code = "";
            foreach(var input in AuthenticationPanelCodeFields)
            {
                code += input.text;
            }
            ModIOUnity.SubmitEmailSecurityCode(code, CodeSubmitted);
        }
#endregion
        
#region Third party authentication submissions
        internal void SubmitSteamAuthenticationRequest()
        {
            OpenAuthenticationPanel_Waiting();

            ModIOUnity.AuthenticateUserViaSteam(optionalSteamAppTicket, 
                optionalThirdPartyEmailAddressUsedForAuthentication,
                LastReceivedTermsOfUse.hash,
                delegate (Result result)
                {
                    ThirdPartyAuthenticationSubmitted(result, UserPortal.Steam);
                });
        }

        internal void SubmitXboxAuthenticationRequest()
        {
            OpenAuthenticationPanel_Waiting();

            ModIOUnity.AuthenticateUserViaXbox(optionalXboxToken, 
                optionalThirdPartyEmailAddressUsedForAuthentication,
                LastReceivedTermsOfUse.hash,
                delegate (Result result)
                {
                    ThirdPartyAuthenticationSubmitted(result, UserPortal.XboxLive);
                });
        }

        internal void SubmitSwitchAuthenticationRequest()
        {
            OpenAuthenticationPanel_Waiting();

            ModIOUnity.AuthenticateUserViaSwitch(optionalSwitchToken, 
                optionalThirdPartyEmailAddressUsedForAuthentication,
                LastReceivedTermsOfUse.hash,
                delegate (Result result)
                {
                    ThirdPartyAuthenticationSubmitted(result, UserPortal.Nintendo);
                });
        }
#endregion // Third party authentication submissions
        
#region Receive Response
        internal void EmailSent(Result result)
        {
            if(result.Succeeded())
            {
                OpenAuthenticationPanel_Code();
            }
            else
            {
                if(result.IsInvalidEmailAddress())
                {
                    OpenAuthenticationPanel_Problem("That does not appear to be a valid email. Please check your email and try again."
                                                    + " address.", "Invalid email address",
                        OpenAuthenticationPanel_Email);
                } 
                else
                {
                    Debug.LogError("something else wrong email");
                    OpenAuthenticationPanel_Problem("Make sure you entered a valid email address and"
                                                    + " that you are still connected to the internet"
                                                    + " before trying again.", "Something went wrong",
                        OpenAuthenticationPanel_Email);
                }
            }
        }

        internal void ReceiveTermsOfUse(ResultAnd<TermsOfUse> resultAndtermsOfUse)
        {
            if (resultAndtermsOfUse.result.Succeeded())
            {
                CacheTermsOfUseAndLinks(resultAndtermsOfUse.value);
                OpenAuthenticationPanel_TermsOfUse(resultAndtermsOfUse.value.termsOfUse);
            }
            else
            {
                OpenAuthenticationPanel_Problem("Unable to connect to the mod.io server. Please"
                                                + " check your internet connection before retrying.",
                                                "Something went wrong",
                                                CloseAuthenticationPanel);
            }
        }

        internal void CodeSubmitted(Result result)
        {
            if(result.Succeeded())
            {
                OpenAuthenticationPanel_Complete();
                ModIOUnity.FetchUpdates(delegate { });
                ModIOUnity.EnableModManagement(ModManagementEvent);
                if(ModDetailsPanel.activeSelf)
                {
                    UpdateModDetailsSubscribeButtonText();
                }
            }
            else
            {
                if (result.IsInvalidSecurityCode())
                {
                    OpenAuthenticationPanel_Problem("The code that you entered did not match the "
                                                    + "one sent to the email address you provided. "
                                                    + "Please check you entered the code correctly.",
                                                    "Invalid code",
                                                    OpenAuthenticationPanel_Code);
                }
                else
                {
                    OpenAuthenticationPanel_Problem();
                }
            }
        }

        internal void CacheTermsOfUseAndLinks(TermsOfUse TOS)
        {
            LastReceivedTermsOfUse = TOS;
            foreach(var link in TOS.links)
            {
                if(link.name == "Terms of Use")
                {
                    termsOfUseURL = link.url;
                } 
                else if(link.name == "Privacy Policy")
                {
                    privacyPolicyURL = link.url;
                }
            }
        }

        internal void ThirdPartyAuthenticationSubmitted(Result result, UserPortal authenticationPortal)
        {
            if(result.Succeeded())
            {
                currentAuthenticationPortal = authenticationPortal;
                OpenAuthenticationPanel_Complete();
                ModIOUnity.FetchUpdates(delegate { });
                ModIOUnity.EnableModManagement(ModManagementEvent);
                if(ModDetailsPanel.activeSelf)
                {
                    UpdateModDetailsSubscribeButtonText();
                }
            }
            else
            {
                currentAuthenticationPortal = UserPortal.None;
                OpenAuthenticationPanel_Problem("We were unable to validate your credentials with"
                                                + " the mod.io server.");
            }
        }
#endregion
        
#region Avatar
        internal async void SetupUserAvatar()
        {
            switch(currentAuthenticationPortal)
            {
                case UserPortal.Steam:
                    SetAvatarSprite(SteamAvatar);
                    break;
                case UserPortal.XboxLive:
                    SetAvatarSprite(XboxAvatar);
                    break;
                default:
                    await GetCurrentUser();
                    DownloadAvatar();
                    break;
            }
        }

        internal async Task GetCurrentUser()
        {
            ResultAnd<UserProfile> resultAnd = await ModIOUnityAsync.GetCurrentUser();
            
            if (resultAnd.result.Succeeded())
            {
                currentUserProfile = resultAnd.value;
            }
        }

        internal async void DownloadAvatar()
        {
            ResultAnd<Texture2D> resultTexture = await ModIOUnityAsync.DownloadTexture(currentUserProfile.avatar_50x50);
            
            if(resultTexture.result.Succeeded())
            {
                // convert texture 2D into sprite for the Image component
                Sprite sprite = Sprite.Create(resultTexture.value, 
                    new Rect(0, 0, resultTexture.value.width, resultTexture.value.height), Vector2.zero);
                SetAvatarSprite(sprite);
            }
            else
            {
                // If this failed we turn off the renderer so the default icon can be displayed behind it
                Avatar_Main.gameObject.SetActive(false);
            }
        }

        // This might be worth setting up as a Message
        internal void SetAvatarSprite(Sprite sprite)
        {
            if (currentAuthenticationPortal == UserPortal.None)
            {
                // turn on main avatar image
                Avatar_Main.gameObject.SetActive(true);
                Avatar_DownloadQueue.gameObject.SetActive(true);
                
                // turn off platform icon
                PlatformIcon_Main.transform.parent.gameObject.SetActive(false);
                PlatformIcon_DownloadQueue.transform.parent.gameObject.SetActive(false);
                
                // change sprites
                Avatar_Main.sprite = sprite;
                Avatar_DownloadQueue.sprite = sprite;
            }
            else
            {
                // turn off main avatar icons
                Avatar_Main.gameObject.SetActive(false);
                Avatar_DownloadQueue.gameObject.SetActive(false);
                
                // turn on platform icon
                PlatformIcon_Main.transform.parent.gameObject.SetActive(true);
                PlatformIcon_DownloadQueue.transform.parent.gameObject.SetActive(true);
                
                // change sprites
                PlatformIcon_Main.sprite = sprite;
                PlatformIcon_DownloadQueue.sprite = sprite;
            }
        }
#endregion

    }
}
