using System.Threading.Tasks;
using ModIO;
using ModIO.Util;
using UnityEngine;

namespace ModIOBrowser.Implementation
{
    public partial class Authentication : SelfInstancingMonoSingleton<Authentication>
    {
        public bool IsAuthenticated = false;

        internal static string optionalThirdPartyEmailAddressUsedForAuthentication;

        internal static PlayStationEnvironment PSEnvironment;

        internal static Browser.RetrieveAuthenticationCodeDelegate getSteamAppTicket;
        internal static Browser.RetrieveAuthenticationCodeDelegate getXboxToken;
        internal static Browser.RetrieveAuthenticationCodeDelegate getSwitchToken;
        internal static Browser.RetrieveAuthenticationCodeDelegate getPlayStationAuthCode;
        internal static Browser.RetrieveAuthenticationCodeDelegate getEpicAuthCode;
        internal static Browser.RetrieveAuthenticationCodeDelegate getGogAuthCode;

        public ExternalAuthenticationToken currentAuthToken;
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
        public void SendRequestExternalAuthentication()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();
            ModIOUnity.RequestExternalAuthentication(ReceivedExternalAuthenticationToken);
        }

        public void ReceivedExternalAuthenticationToken(ResultAnd<ExternalAuthenticationToken> response)
        {
            if(response.result.Succeeded())
            {
                currentAuthToken = response.value;
                AuthenticationPanels.Instance.OpenPanel_ExternalAuthentication(response.value);
            }
            else
            {
                AuthenticationPanels.Instance.OpenPanel_Problem(
                    null, "could not connect", AuthenticationPanels.Instance.OpenPanel_TermsOfUse);
            }
        }

        public void HyperLinkToExternalLogin()
        {
            WebBrowser.OpenWebPage($"{currentAuthToken.url}?code={currentAuthToken.code}");
        }

        public void CancelExternalAuthenticationRequest()
        {
            AuthenticationPanels.Instance.Close();
            currentAuthToken.Cancel();
        }

        public void CopyExternalAuthenticationCodeToClipboard()
        {
            GUIUtility.systemCopyBuffer = currentAuthToken.code;
            Notifications.Instance.AddNotificationToQueue(new Notifications.QueuedNotice()
            {
                title = "Copied",
                description = "Code copied to clipboard",
                positiveAccent = true,
            });
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

        public void SubmitGogAuthenticationRequest()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();

            getGogAuthCode((token) =>
            {
                MonoDispatcher.Instance.Run(() =>
                {
                    if(string.IsNullOrEmpty(token))
                    {
                        currentAuthenticationPortal = UserPortal.None;
                        AuthenticationPanels.Instance.OpenPanel_Problem("We were unable to validate your credentials with the mod.io server.");
                        return;
                    }

                    ModIOUnity.AuthenticateUserViaGOG(token,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        LastReceivedTermsOfUse.hash,
                        delegate (Result result)
                        {
                            ThirdPartyAuthenticationSubmitted(result, UserPortal.GOG);
                        });
                });
            });
        }

        public void SubmitEpicAuthenticationRequest()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();

            getEpicAuthCode((token) =>
            {
                MonoDispatcher.Instance.Run(() =>
                {
                    if(string.IsNullOrEmpty(token))
                    {
                        currentAuthenticationPortal = UserPortal.None;
                        AuthenticationPanels.Instance.OpenPanel_Problem("We were unable to validate your credentials with the mod.io server.");
                        return;
                    }

                    ModIOUnity.AuthenticateUserViaEpic(token,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        LastReceivedTermsOfUse.hash,
                        delegate (Result result)
                        {
                            ThirdPartyAuthenticationSubmitted(result, UserPortal.EpicGamesStore);
                        });
                });
            });
        }

        public void SubmitSteamAuthenticationRequest()
        {
            AuthenticationPanels.Instance.OpenPanel_Waiting();

            getSteamAppTicket.Invoke((appTicket) =>
            {
                MonoDispatcher.Instance.Run(() =>
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
                MonoDispatcher.Instance.Run(() =>
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
                MonoDispatcher.Instance.Run(() =>
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
                MonoDispatcher.Instance.Run(() =>
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
        
        public static async Task GetNewAccessToken()
        {
            //Re-cache the TOS because we need the hash
            var response = await ModIOUnityAsync.GetTermsOfUse();
            TermsHash hash = default;
            if(response.result.Succeeded())
            {
                hash = response.value.hash;
            }

            // Use TCS to wait for the callbacks inside the following blocks to complete so we can
            // inform the original invocation point when the access token has been renewed (or attempted to)
            TaskCompletionSource<bool> callbackTcs = new TaskCompletionSource<bool>();

            if(getSteamAppTicket != null)
            {
                getSteamAppTicket(appTicket =>
                {
                    ModIOUnity.AuthenticateUserViaSteam(appTicket,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        hash,
                        delegate
                        {
                            Instance.currentAuthenticationPortal = UserPortal.Steam;
                            ModIOUnity.EnableModManagement(Mods.ModManagementEvent);
                            callbackTcs.SetResult(true);
                        });
                });
                await callbackTcs.Task;
            }
            else if(getXboxToken != null)
            {
                getXboxToken(token =>
                {
                    ModIOUnity.AuthenticateUserViaXbox(token,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        hash,
                        delegate
                        {
                            Instance.currentAuthenticationPortal = UserPortal.XboxLive;
                            ModIOUnity.EnableModManagement(Mods.ModManagementEvent);
                            callbackTcs.SetResult(true);
                        });
                });
                await callbackTcs.Task;
            }
            else if(getSwitchToken != null)
            {
                getSwitchToken(token =>
                {
                    ModIOUnity.AuthenticateUserViaSwitch(token,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        hash,
                        delegate
                        {
                            Instance.currentAuthenticationPortal = UserPortal.Nintendo;
                            ModIOUnity.EnableModManagement(Mods.ModManagementEvent);
                            callbackTcs.SetResult(true);
                        });
                });
                await callbackTcs.Task;
            }
            else if(getPlayStationAuthCode != null)
            {
                getPlayStationAuthCode(authCode =>
                {
                    ModIOUnity.AuthenticateUserViaPlayStation(authCode,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        hash,
                        PSEnvironment,
                        delegate
                        {
                            Instance.currentAuthenticationPortal = UserPortal.PlayStationNetwork;
                            ModIOUnity.EnableModManagement(Mods.ModManagementEvent);
                            callbackTcs.SetResult(true);
                        });
                });
                await callbackTcs.Task;
            }
            else if(getGogAuthCode != null)
            {
                getGogAuthCode(authCode =>
                {
                    ModIOUnity.AuthenticateUserViaGOG(authCode,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        hash,
                        delegate
                        {
                            Instance.currentAuthenticationPortal = UserPortal.GOG;
                            ModIOUnity.EnableModManagement(Mods.ModManagementEvent);
                            callbackTcs.SetResult(true);
                        });
                });
                await callbackTcs.Task;
            }
            else if(getEpicAuthCode != null)
            {
                getEpicAuthCode(authCode =>
                {
                    ModIOUnity.AuthenticateUserViaEpic(authCode,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        hash,
                        delegate
                        {
                            Instance.currentAuthenticationPortal = UserPortal.EpicGamesStore;
                            ModIOUnity.EnableModManagement(Mods.ModManagementEvent);
                            callbackTcs.SetResult(true);
                        });
                });
                await callbackTcs.Task;
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
                Home.Instance.RefreshHomePanel();
                
                currentAuthenticationPortal = authenticationPortal;
                AuthenticationPanels.Instance.OpenPanel_Complete();
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
