using System;
using UnityEngine;

#if UNITY_IOS
using System.Text;
using AppleAuth;
using AppleAuth.Enums;
using AppleAuth.Extensions;
using AppleAuth.Interfaces;
#endif

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformExampleIos : MonoBehaviour
    {
        const string AppleUserIdKey = "AppleUserIdKey";
        const string AppleEmailKey = "AppleEmailKey";
        const string AppleFullNameKey = "AppleFullNameKey";
        
#if UNITY_IOS
        IAppleIDCredential _appleIdCredential;
        IPasswordCredential _passwordCredential;
        IAppleAuthManager _appleAuthManager;

        private void Awake()
        {
            _appleAuthManager.QuickLogin(new AppleAuthQuickLoginArgs(), (credential)=>
            {
                Logger.Log(LogLevel.Verbose, "Received a valid credential!");

                // Previous Apple sign in credential
                _appleIdCredential = credential as IAppleIDCredential;

                // Saved Keychain credential (read about Keychain Items)
                _passwordCredential = credential as IPasswordCredential;
                
                // --- This is the important line to include in your own implementation ---
                ModioPlatformIos.SetAsPlatform(_appleIdCredential);
                
            }, (quickLoginError)=>
            {
                Logger.Log(LogLevel.Error, $"Quick login failed. The user has never used Sign in With Apple on your app. {quickLoginError}");
                LoginWithAppleId((success, fullLoginError) =>
                {
                    if (success)
                    {
                        // --- This is the important line to include in your own implementation ---
                        ModioPlatformIos.SetAsPlatform(_appleIdCredential);
                    }
                    Logger.Log(LogLevel.Error, $"Login failed: {fullLoginError}");

                });
            });
        }
        
        void LoginWithAppleId(Action<bool, IAppleError> callback)
        {
            var loginArgs = new AppleAuthLoginArgs(LoginOptions.IncludeEmail | LoginOptions.IncludeFullName);

            _appleAuthManager.LoginWithAppleId(
                loginArgs,
                credential =>
                {
                    // Obtained credential, cast it to IAppleIDCredential
                    _appleIdCredential = credential as IAppleIDCredential;
                    if (_appleIdCredential != null)
                    {
                        // Apple User ID
                        // You should save the user ID somewhere in the device
                        var userId = _appleIdCredential.User;

                        // Email (Received ONLY in the first login)
                        var email = _appleIdCredential.Email;

                        // Full name (Received ONLY in the first login)
                        var fullName = _appleIdCredential.FullName;

                        // Identity token
                        var identityToken = Encoding.UTF8.GetString(
                            _appleIdCredential.IdentityToken,
                            0,
                            _appleIdCredential.IdentityToken.Length);

                        // Authorization code
                        var authorizationCode = Encoding.UTF8.GetString(
                            _appleIdCredential.AuthorizationCode,
                            0,
                            _appleIdCredential.AuthorizationCode.Length);

                        Logger.Log(LogLevel.Verbose, $"EMAIL: {email}, NAME: {fullName.Nickname}, USERID: {userId}, IDTOKEN: {identityToken}, AUTHCODE: {authorizationCode}");

                        // And now you have all the information to create/login a user in your system
                        callback?.Invoke(true, null);
                    }
                    else
                    {
                        callback?.Invoke(false, null);
                    }
                },
                error =>
                {
                    // Something went wrong
                    var authorizationErrorCode = error.GetAuthorizationErrorCode();
                    Logger.Log(LogLevel.Error, $"Error Code: {authorizationErrorCode}");
                    callback?.Invoke(false, error);
                });
        }
#endif
    }
}
