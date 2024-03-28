
#if UNITY_FACEPUNCH
using Steamworks;
#endif
using System;
using UnityEngine;
using ModIOBrowser;

public class FacepunchExample : MonoBehaviour
{
    [SerializeField] int appId;
#if UNITY_FACEPUNCH
    void Awake()
    {
        try
        {
            SteamClient.Init((uint)appId, true);
        }
        catch(System.Exception e)
        {
            Debug.Log(e);
        }

        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        async void CallbackOnReceiveCode(Action<string> action)
        {
            byte[] encryptedAppTicket = await SteamUser.RequestEncryptedAppTicketAsync();
            string base64Ticket = ModIO.Util.Utility.EncodeEncryptedSteamAppTicket(encryptedAppTicket, (uint)encryptedAppTicket.Length);
            action?.Invoke(base64Ticket);
        }

        Browser.SetupSteamAuthenticationOption(CallbackOnReceiveCode);
    }

    void OnDisable()
    {
        SteamClient.Shutdown();
    }

    void Update()
    {
        SteamClient.RunCallbacks();
    }
#endif
}
