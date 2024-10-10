#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
    #define DISABLESTEAMWORKS
#elif UNITY_FACEPUNCH
    #define DISABLESTEAMWORKS
#endif

using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_STEAMWORKS && !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformSteamworks : ModioPlatform, IModioSsoPlatform
    {
        private static uint appId;
        private string betaStoreHomePage => $"https://store.steampowered.com/app/{appId}/modio/?beta=1";
        private string betaStoreInventoryPage => $"https://store.steampowered.com/itemstore/{appId}/browse/?filter=all";

        public static void SetAsPlatform(uint appId)
        {
            ModioPlatformSteamworks.appId = appId;
            ActivePlatform = new ModioPlatformSteamworks();
        }

        public async void PerformSso(TermsHash? displayedTerms, Action<Result> onComplete, string optionalThirdPartyEmailAddressUsedForAuthentication = null)
        {
#if UNITY_STEAMWORKS && !DISABLESTEAMWORKS
            var hresult = SteamUser.RequestEncryptedAppTicket(null, 0);
            CallResult<EncryptedAppTicketResponse_t> encryptedAppTicketResponseCallResult = CallResult<EncryptedAppTicketResponse_t>.Create(OnEncryptedAppTicketResponseCallResult);
            encryptedAppTicketResponseCallResult.Set(hresult, (response, failure) =>
            {
                int cbMaxTicket = 1024;
                byte[] pTicket = new byte[1024];
                if (SteamUser.GetEncryptedAppTicket(pTicket, cbMaxTicket, out uint pcbTicket))
                {
                    string base64Ticket = ModIO.Util.Utility.EncodeEncryptedSteamAppTicket(pTicket, pcbTicket);
                    ModIOUnity.AuthenticateUserViaSteam(base64Ticket,
                        optionalThirdPartyEmailAddressUsedForAuthentication,
                        displayedTerms,
                        (r) =>
                        {
                            onComplete?.Invoke(r);
                        });
                }
                else
                {
                    Logger.Log(LogLevel.Error, "Failed to get Encrypted App Ticket!");
                    onComplete?.Invoke(ResultBuilder.Unknown);
                }
            });
#endif
        }

#if UNITY_STEAMWORKS && !DISABLESTEAMWORKS
        private void OnEncryptedAppTicketResponseCallResult(EncryptedAppTicketResponse_t response, bool ioFailure)
        {
            if (response.m_eResult == EResult.k_EResultOK)
                Logger.Log(LogLevel.Verbose, "Purchase Completed!");
        }
#endif

        public override async Task<Result> OpenPlatformPurchaseFlow()
        {
#if UNITY_STEAMWORKS && !DISABLESTEAMWORKS
            SteamFriends.ActivateGameOverlayToWebPage(betaStoreInventoryPage, EActivateGameOverlayToWebPageMode.k_EActivateGameOverlayToWebPageMode_Modal);

            float timeoutAt = Time.unscaledTime + 5f;
            while (!SteamUtils.IsOverlayEnabled() && Time.unscaledTime < timeoutAt)
            {
                await Task.Yield();
            }

            if (!SteamUtils.IsOverlayEnabled())
            {
                Logger.Log(LogLevel.Error, "Steam overlay never opened");
                return ResultBuilder.Unknown;
            }

            while (SteamUtils.IsOverlayEnabled())
            {
                await Task.Yield();
            }

            return ResultBuilder.Success;
#else
            return ResultBuilder.Unknown;
#endif
        }

        public override void OpenWebPage(string url)
        {
#if UNITY_STEAMWORKS && !DISABLESTEAMWORKS
            SteamFriends.ActivateGameOverlayToWebPage(this.betaStoreHomePage, EActivateGameOverlayToWebPageMode.k_EActivateGameOverlayToWebPageMode_Modal);
#else
            base.OpenWebPage(url);
#endif
        }
    }
}
