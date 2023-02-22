using ModIO;
using ModIO.Util;
using ModIOBrowser.Implementation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    public partial class Authentication : SimpleMonoSingleton<Authentication>
    {
        public bool IsAuthenticated = false;
        
        internal static string optionalThirdPartyEmailAddressUsedForAuthentication;

        internal static PlayStationEnvironment PSEnvironment;
        
        internal static Browser.RetrieveAuthenticationCodeDelegate getSteamAppTicket;
        internal static Browser.RetrieveAuthenticationCodeDelegate getXboxToken;
        internal static Browser.RetrieveAuthenticationCodeDelegate getSwitchToken;
        internal static Browser.RetrieveAuthenticationCodeDelegate getPlayStationAuthCode;

        public UserProfile currentUserProfile;
        public TermsOfUse LastReceivedTermsOfUse;
        public string privacyPolicyURL;
        public string termsOfUseURL;

        public UserPortal currentAuthenticationPortal = UserPortal.None;
        
        #region Send Request
        public void GetTermsOfUse()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();
            ModIOUnity.GetTermsOfUse(ReceiveTermsOfUse);
        }
        public void SendEmail()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();
            ModIOUnity.RequestAuthenticationEmail(AuthenticationPanels.Instance.AuthenticationPanelEmailField.text, EmailSent);
        }

        public void SubmitAuthenticationCode()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();
            string code = "";
            foreach(var input in AuthenticationPanels.Instance.AuthenticationPanelCodeFields)
            {
                code += input.text;
            }
            ModIOUnity.SubmitEmailSecurityCode(code, CodeSubmitted);
        }
        #endregion

        #region Third Party

        public void SubmitSteamAuthenticationRequest()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();

            getSteamAppTicket.Invoke((appTicket) =>
            {
                Dispatcher.Instance.Run(() =>
                {
                    if(string.IsNullOrEmpty(appTicket))
                    {
                        currentAuthenticationPortal = UserPortal.None;
                        AuthenticationPanels.Instance.OpenPanel_Problem("We were unable to validate your credentials with the mod.io server.");
                        return;
                    }

                    ModIOUnity.AuthenticateUserViaSteam(appTicket,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        LastReceivedTermsOfUse.hash,
                        delegate (Result result)
                        {
                            ThirdPartyAuthenticationSubmitted(result, UserPortal.Steam);
                        });
                });
            });
        }

        public void SubmitXboxAuthenticationRequest()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();

            getXboxToken((token) =>
            {
                Dispatcher.Instance.Run(() =>
                {
                    if(string.IsNullOrEmpty(token))
                    {
                        currentAuthenticationPortal = UserPortal.None;
                        AuthenticationPanels.Instance.OpenPanel_Problem("We were unable to validate your credentials with the mod.io server.");
                        return;
                    }

                    ModIOUnity.AuthenticateUserViaXbox(token,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        LastReceivedTermsOfUse.hash,
                        delegate (Result result)
                        {
                            ThirdPartyAuthenticationSubmitted(result, UserPortal.XboxLive);
                        });
                });
            });
        }

        public void SubmitSwitchAuthenticationRequest()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();

            getSwitchToken((token) =>
            {
                Dispatcher.Instance.Run(() =>
                {
                    if(string.IsNullOrEmpty(token))
                    {
                        currentAuthenticationPortal = UserPortal.None;
                        AuthenticationPanels.Instance.OpenPanel_Problem("We were unable to validate your credentials with the mod.io server.");
                        return;
                    }

                    ModIOUnity.AuthenticateUserViaSwitch(token,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        LastReceivedTermsOfUse.hash,
                        delegate (Result result)
                        {
                            ThirdPartyAuthenticationSubmitted(result, UserPortal.Nintendo);
                        });
                });
            });
        }

        internal void SubmitPlayStationAuthenticationRequest()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();

            getPlayStationAuthCode((authCode) =>
            {
                Dispatcher.Instance.Run(() =>
                {
                    if(string.IsNullOrEmpty(authCode))
                    {
                        currentAuthenticationPortal = UserPortal.None;
                        AuthenticationPanels.Instance.OpenPanel_Problem("We were unable to validate your credentials with the mod.io server.");
                        return;
                    }

                    ModIOUnity.AuthenticateUserViaPlayStation(authCode,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        LastReceivedTermsOfUse.hash, PSEnvironment,
                        delegate (Result result)
                        {
                            ThirdPartyAuthenticationSubmitted(result, UserPortal.PlayStationNetwork);
                        });
                });
            });
        }

        #endregion

        #region misc ui
        public void Close() => AuthenticationPanels.Instance.Close();

        public void HyperLinkToTOS() => AuthenticationPanels.Instance.HyperLinkToTOS();
        
        public void HyperLinkToPrivacyPolicy() => AuthenticationPanels.Instance.HyperLinkToPrivacyPolicy();

        void Logout()
        {
            if(ModIOUnity.LogOutCurrentUser().Succeeded())
            {
                Avatar.Instance.Avatar_Main.gameObject.SetActive(false);
                IsAuthenticated = false;
                Close();
            }
            else
            {
                // TODO inform the user if this failed (Which really shouldn't ever fail)
            }
        }
        #endregion

        #region Recieve Response
        internal void EmailSent(Result result)
        {
            if(result.Succeeded())
            {
                AuthenticationPanels.Instance.OpenPanel_Code();
            }
            else
            {
                if(result.IsInvalidEmailAddress())
                {

                    AuthenticationPanels.Instance.OpenPanel_Problem(
                        "That does not appear to be a valid email. Please check your email address and try again.",
                        "Invalid email address",
                        AuthenticationPanels.Instance.OpenPanel_Email);
                }
                else
                {
                    Debug.LogError("something else wrong email");
                    AuthenticationPanels.Instance.OpenPanel_Problem(
                        "Make sure you entered a valid email address and that you are still connected to the internet before trying again.",
                        "Something went wrong",
                        AuthenticationPanels.Instance.OpenPanel_Email);
                }
            }
        }

        internal void ReceiveTermsOfUse(ResultAnd<TermsOfUse> resultAndTermsOfUse)
        {
            if(resultAndTermsOfUse.result.Succeeded())
            {
                CacheTermsOfUseAndLinks(resultAndTermsOfUse.value);
                AuthenticationPanels.Instance.OpenPanel_TermsOfUse(resultAndTermsOfUse.value.termsOfUse);
            }
            else
            {
                AuthenticationPanels.Instance.OpenPanel_Problem(
                    "Unable to connect to the mod.io server. Please check your internet connection before retrying.",
                    "Something went wrong",
                    Close);
            }
        }

        public void CodeSubmitted(Result result)
        {
            if(result.Succeeded())
            {
                AuthenticationPanels.Instance.OpenPanel_Complete();
                ModIOUnity.FetchUpdates(delegate { });
                ModIOUnity.EnableModManagement(Mods.ModManagementEvent);
                if(Details.IsOn())
                {
                    Details.Instance.UpdateSubscribeButtonText();
                }
            }
            else
            {
                if(result.IsInvalidSecurityCode())
                {
                    AuthenticationPanels.Instance.OpenPanel_Problem(
                        "The code that you entered did not match the one sent to the email address you provided. Please check you entered the code correctly.",
                        "Invalid code",
                        AuthenticationPanels.Instance.OpenPanel_Code);
                }
                else
                {
                    AuthenticationPanels.Instance.OpenPanel_Problem();
                }
            }
        }

        void CacheTermsOfUseAndLinks(TermsOfUse TOS)
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

        void ThirdPartyAuthenticationSubmitted(Result result, UserPortal authenticationPortal)
        {
            if(result.Succeeded())
            {
                currentAuthenticationPortal = authenticationPortal;
                AuthenticationPanels.Instance.OpenPanel_Complete();
                ModIOUnity.FetchUpdates(delegate { });
                ModIOUnity.EnableModManagement(Mods.ModManagementEvent);
                if(Details.IsOn())
                {
                    Details.Instance.UpdateSubscribeButtonText();
                }
            }
            else
            {
                currentAuthenticationPortal = UserPortal.None;
                AuthenticationPanels.Instance.OpenPanel_Problem("We were unable to validate your credentials with the mod.io server.");
            }
        }
        #endregion
    }
}
