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
        public static void SetAsPlatform()
        {
            ActivePlatform = new ModioPlatformFacepunch();
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
            SteamFriends.OpenStoreOverlay(SteamClient.AppId);

            float timeoutAt = Time.unscaledTime + 5f;
            while (!SteamUtils.IsOverlayEnabled && Time.unscaledTime < timeoutAt)
            {
                await Task.Yield();
            }

            if (!SteamUtils.IsOverlayEnabled)
            {
                Logger.Log(LogLevel.Error, "Steam overlay never opened");
                return ResultBuilder.Unknown;
            }

            while (SteamUtils.IsOverlayEnabled)
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
