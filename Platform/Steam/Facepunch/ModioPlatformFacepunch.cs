using System;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_FACEPUNCH
using Steamworks;
#endif

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformFacepunch : ModioPlatform, IModioSsoPlatform
    {
        static bool OverlayActive { get; set; }
        public static void SetAsPlatform()
        {
            ActivePlatform = new ModioPlatformFacepunch();
            #if UNITY_FACEPUNCH
            SteamFriends.OnGameOverlayActivated += OnGameOverlayActiveStateChanged;
            #endif
        }
        static void OnGameOverlayActiveStateChanged(bool overlayActive)
        {
            OverlayActive = overlayActive;
        }

        public async void PerformSso(TermsHash? displayedTerms, Action<Result> onComplete, string optionalThirdPartyEmailAddressUsedForAuthentication = null)
        {
#if UNITY_FACEPUNCH

            byte[] encryptedAppTicket = await SteamUser.RequestEncryptedAppTicketAsync();
            string base64Ticket = Util.Utility.EncodeEncryptedSteamAppTicket(encryptedAppTicket, (uint)encryptedAppTicket.Length);

            ModIOUnity.AuthenticateUserViaSteam(base64Ticket,
                optionalThirdPartyEmailAddressUsedForAuthentication,
                displayedTerms,
                onComplete);
#endif
        }

        public override async Task<Result> OpenPlatformPurchaseFlow()
        {
#if UNITY_FACEPUNCH
            if (!SteamClient.IsValid)
            {
                Logger.Log(LogLevel.Error, "Steam client is not valid.");
                return ResultBuilder.Unknown;
            }

            if (!SteamUtils.IsOverlayEnabled)
            {
                Logger.Log(LogLevel.Error, "Steam overlay is not enabled, or was accessed too early.");
                return ResultBuilder.Unknown;
            }

            SteamFriends.OpenStoreOverlay(SteamClient.AppId);

            float timeoutAt = Time.unscaledTime + 5f;

            while (!OverlayActive && Time.unscaledTime < timeoutAt)
            {
                await Task.Yield();
            }

            if (!OverlayActive)
            {
                Logger.Log(LogLevel.Error, "Steam overlay never opened");
                return ResultBuilder.Unknown;
            }

            while (OverlayActive)
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
#if UNITY_FACEPUNCH
            SteamFriends.OpenWebOverlay(url);
#else
            base.OpenWebPage(url);
#endif
        }
    }
}
