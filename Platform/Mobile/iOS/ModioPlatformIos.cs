﻿using UnityEngine;
using System;
using System.Text;

#if UNITY_IOS
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
#endif

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformIos : ModioPlatform, IModioSsoPlatform
    {
#if UNITY_IOS
        IAppleIDCredential _appleIdCredential;
        public static void SetAsPlatform(IAppleIDCredential appleIdCredential)
        {

            ActivePlatform = new ModioPlatformIos
            {
                _appleIdCredential = appleIdCredential
            };
        }

        // Callback function for receiving authentication token
        void GetToken(Action<string> onReceivedIdToken)
        {
            try
            {
                if (_appleIdCredential != null && _appleIdCredential.IdentityToken != null)
                {
                    Logger.Log(LogLevel.Verbose, "appleIdCredential valid");

                    // Convert identity token to string
                    var idToken = Encoding.UTF8.GetString(_appleIdCredential.IdentityToken, 0,
                        _appleIdCredential.IdentityToken.Length);
                    onReceivedIdToken?.Invoke(idToken);
                }

            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, e.Message);
            }
        }
#endif
        public void PerformSso(TermsHash? displayedTerms, Action<Result> onComplete,
            string optionalThirdPartyEmailAddressUsedForAuthentication = null)
        {
#if UNITY_IOS
            GetToken((idToken) =>
            {
                ModIOUnity.AuthenticateUserViaApple(idToken,
                    optionalThirdPartyEmailAddressUsedForAuthentication,
                    displayedTerms, 
                    onComplete);
            });
#endif
        }
    }
}