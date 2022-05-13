using System;
using System.Collections;
using ModIO;
using ModIO.Implementation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    public partial class Browser : MonoBehaviour
    {
        [Header("Authentication Panel")]
        [SerializeField] GameObject AuthenticationPanel;
        [SerializeField] GameObject AuthenticationPanelWaitingForResponseAnimation;
        [SerializeField] GameObject AuthenticationPanelEnterEmail;
        [SerializeField] TMP_InputField AuthenticationPanelEmailField;
        [SerializeField] GameObject AuthenticationPanelEnterCode;
        [SerializeField] TMP_InputField[] AuthenticationPanelCodeFields;
        [SerializeField] Button AuthenticationPanelConnectViaSteamButton;
        [SerializeField] Button AuthenticationPanelConnectViaEmailButton;
        [SerializeField] Button AuthenticationPanelBackButton;
        [SerializeField] Button AuthenticationPanelAgreeButton;
        [SerializeField] Button AuthenticationPanelSendCodeButton;
        [SerializeField] Button AuthenticationPanelSubmitButton;
        [SerializeField] Button AuthenticationPanelCompletedButton;
        [SerializeField] GameObject AuthenticationPanelTermsOfUseLinks;
        [SerializeField] TMP_Text AuthenticationPanelTitleText;
        [SerializeField] TMP_Text AuthenticationPanelInfoText;
        [SerializeField] Image Avatar;
        [SerializeField] Image AvatarDownloadBar;

        UserProfile currentUserProfile;
        TermsOfUse LastReceivedTermsOfUse;
        string privacyPolicyURL;
        string termsOfUseURL;

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
            
            // Get the default selection
            if(CollectionPanel.activeSelf)
            {
                CollectionPanelFirstDropDownFilter.Select();
            } 
            else if(ModDetailsPanel.activeSelf)
            {
                defaultModDetailsSelection.Select();
            } 
            else
            {
                defaultBrowserSelection.Select();
            }
        }

        public void OpenAuthenticationPanel()
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);

            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Authentication";

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = "mod.io is a 3rd party utility that provides access to a mod workshop. Choose how you wish to be authenticated.";
            
            // TODO implement steam authentication
            //AuthenticationPanelConnectViaSteamButton.gameObject.SetActive(true);
            //AuthenticationPanelConnectViaSteamButton.onClick.RemoveAllListeners();
            //AuthenticationPanelConnectViaSteamButton.onClick.AddListener(GetTermsOfUse);
            
            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(CloseAuthenticationPanel);

            AuthenticationPanelConnectViaEmailButton.gameObject.SetActive(true);
            AuthenticationPanelConnectViaEmailButton.onClick.RemoveAllListeners();
            AuthenticationPanelConnectViaEmailButton.onClick.AddListener(GetTermsOfUse);
            
            AuthenticationPanelConnectViaEmailButton.Select();
        }

        internal void HideAuthenticationPanelObjects()
        {
            AuthenticationPanelTitleText.gameObject.SetActive(false);
            AuthenticationPanelInfoText.gameObject.SetActive(false);
            TextAlignmentOptions alignment = AuthenticationPanelInfoText.alignment;
            alignment = TextAlignmentOptions.Left;
            AuthenticationPanelInfoText.alignment = alignment;
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
            AuthenticationPanelCompletedButton.gameObject.SetActive(false);
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
            
            AuthenticationPanelWaitingForResponseAnimation.SetActive(true);
            
            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = "Waiting for response...";
            TextAlignmentOptions alignment = AuthenticationPanelInfoText.alignment;
            alignment = TextAlignmentOptions.Center;
            AuthenticationPanelInfoText.alignment = alignment;
        }

        internal void OpenAuthenticationPanel_Problem(string problem = "", Action onBack = null)
        {
            problem = problem ?? "We were unable to connect to the mod.io server. Check you have a stable internet connection and try again.";
            
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            
            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Something went wrong!";

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = problem;
            
            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            if(onBack == null)
            {
                onBack = CloseAuthenticationPanel;
            }
            AuthenticationPanelBackButton.onClick.AddListener(delegate { onBack();});
            
            AuthenticationPanelBackButton.Select();
        }
        
        internal void OpenAuthenticationPanel_TermsOfUse(string TOS)
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationPanelTermsOfUseLinks.SetActive(true);
            
            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Terms of use";

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = TOS;
            
            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(CloseAuthenticationPanel);

            AuthenticationPanelAgreeButton.gameObject.SetActive(true);
            AuthenticationPanelAgreeButton.onClick.RemoveAllListeners();
            // TODO go to either steam or email auth next
            AuthenticationPanelAgreeButton.onClick.AddListener(OpenAuthenticationPanel_Email);
            
            AuthenticationPanelAgreeButton.Select();
        }
        
        internal void OpenAuthenticationPanel_Email()
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            
            AuthenticationPanelEnterEmail.SetActive(true);
            AuthenticationPanelEmailField.text = "";
            
            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Email authentication";

            AuthenticationPanelBackButton.gameObject.SetActive(true);
            AuthenticationPanelBackButton.onClick.RemoveAllListeners();
            AuthenticationPanelBackButton.onClick.AddListener(CloseAuthenticationPanel);

            AuthenticationPanelSendCodeButton.gameObject.SetActive(true);
            AuthenticationPanelSendCodeButton.onClick.RemoveAllListeners();
            AuthenticationPanelSendCodeButton.onClick.AddListener(SendEmail);
            
            AuthenticationPanelEmailField.Select();
        }

        internal void OpenAuthenticationPanel_Code()
        {
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            AuthenticationPanelEnterCode.SetActive(true);
            for (int i = 0; i < AuthenticationPanelCodeFields.Length; i++)
            {
                if(i == 0)
                {
                    AuthenticationPanelCodeFields[i].Select();
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

            AuthenticationPanelSubmitButton.gameObject.SetActive(true);
            AuthenticationPanelSubmitButton.onClick.RemoveAllListeners();
            AuthenticationPanelSubmitButton.onClick.AddListener(SubmitAuthenticationCode);
            
            //AuthenticationPanelDefaultSelection_Code.Select();
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
                    previous.Select();
                }
                else
                {
                    next.Select();
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
            selectable.Select();
        }
        
        internal void OpenAuthenticationPanel_Complete()
        {
            isAuthenticated = true;
            
            HideAuthenticationPanelObjects();
            AuthenticationPanel.SetActive(true);
            
            AuthenticationPanelTitleText.gameObject.SetActive(true);
            AuthenticationPanelTitleText.text = "Authentication completed";

            AuthenticationPanelInfoText.gameObject.SetActive(true);
            AuthenticationPanelInfoText.text = "You are now connected to the mod.io browser. You can now subscribe to mods to use in your game and track them in your Collection.";
            
            AuthenticationPanelCompletedButton.gameObject.SetActive(true);
            
            AuthenticationPanelCompletedButton.Select();
            
            // Update the user avatar display
            SetupUserAvatar();
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
        
#region Receive Response
        internal void EmailSent(Result result)
        {
            if(result.Succeeded())
            {
                OpenAuthenticationPanel_Code();
            }
            else
            {
                OpenAuthenticationPanel_Problem();
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
                OpenAuthenticationPanel_Problem();
            }
        }

        internal void CodeSubmitted(Result result)
        {
            if(result.Succeeded())
            {
                OpenAuthenticationPanel_Complete();
                ModIOUnity.FetchUpdates(delegate { });
                ModIOUnity.EnableModManagement(ModManagementEvent);
            }
            else
            {
                if (result.IsInvalidSecurityCode())
                {
                    OpenAuthenticationPanel_Problem("Invalid code", OpenAuthenticationPanel_Code);
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
#endregion
        
#region Avatar
        internal void SetupUserAvatar()
        {
            ModIOUnity.GetCurrentUser(CurrentUserReceived);
        }

        internal void CurrentUserReceived(ResultAnd<UserProfile> resultAnd)
        {
            if (resultAnd.result.Succeeded())
            {
                currentUserProfile = resultAnd.value;
                ModIOUnity.DownloadTexture(resultAnd.value.avatar_50x50, DownloadedAvatar);
            }
            else
            {
                Avatar.gameObject.SetActive(false);
            }
        }

        internal void DownloadedAvatar(ResultAnd<Texture2D> resultTexture)
        {
            if(resultTexture.result.Succeeded())
            {
                Sprite sprite = Sprite.Create(resultTexture.value, 
                    new Rect(0, 0, resultTexture.value.width, resultTexture.value.height), Vector2.zero);
                Avatar.gameObject.SetActive(true);
                Avatar.sprite = sprite;
                DownloadQueueAvatarIcon.sprite = sprite;
            }
            else
            {
                Avatar.gameObject.SetActive(false);
            }
        }
#endregion

    }
}
