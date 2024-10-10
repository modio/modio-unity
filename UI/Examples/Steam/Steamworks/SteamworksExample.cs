#if UNITY_STEAMWORKS
using Steamworks;
#endif

using System;
using ModIO;
using UnityEngine;
using ModIOBrowser;
using Logger = ModIO.Implementation.Logger;

public class SteamworksExample : MonoBehaviour
{
#if UNITY_STEAMWORKS
    void Start()
    {
        var hresult = SteamUser.RequestEncryptedAppTicket(null, 0);
        CallResult<EncryptedAppTicketResponse_t> encryptedAppTicketResponseCallResult = CallResult<EncryptedAppTicketResponse_t>.Create(OnEncryptedAppTicketResponseCallResult);
        encryptedAppTicketResponseCallResult.Set(hresult, (response, failure) =>
        {
            int cbMaxTicket = 1024;
            byte[] pTicket = new byte[1024];
            if (SteamUser.GetEncryptedAppTicket(pTicket, cbMaxTicket, out uint pcbTicket))
            {
                string base64Ticket = ModIO.Util.Utility.EncodeEncryptedSteamAppTicket(pTicket, pcbTicket);

                void CallbackOnReceiveCode(Action<string> action)
                {
                    action?.Invoke(base64Ticket);
                }

                Browser.SetupSteamAuthenticationOption(CallbackOnReceiveCode);
            }
        });
    }
    
    private void OnEncryptedAppTicketResponseCallResult(EncryptedAppTicketResponse_t response, bool ioFailure)
    {
        if (response.m_eResult == EResult.k_EResultOK)
            Logger.Log(LogLevel.Verbose, "Purchase Completed!");
    }
#endif
}
