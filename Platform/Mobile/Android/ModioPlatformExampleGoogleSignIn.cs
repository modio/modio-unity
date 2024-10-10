using UnityEngine;

#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google;
#endif

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformExampleGoogleSignIn : MonoBehaviour
    {
        [SerializeField] string clientId = "<your client id here>";
        string idToken;

        private void Awake()
        {
#if UNITY_ANDROID
            SignInSilently();
#endif
        }

#if UNITY_ANDROID
        async void SignInSilently()
        {
            if (Application.isEditor)
                return;

            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = clientId,
                RequestIdToken = true
            };
            GoogleSignIn.Configuration.UseGameSignIn = false;
            GoogleSignIn.Configuration.RequestIdToken = true;
            Logger.Log(LogLevel.Verbose, "Calling SignIn Silently");

            var task = GoogleSignIn.DefaultInstance.SignInSilently();
            await task;
            FinishSignIn(task);
        }

        public async Task SignIn()
        {
            if (Application.isEditor)
                return;

            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                WebClientId = clientId,
                RequestIdToken = true
            };
            GoogleSignIn.Configuration.UseGameSignIn = false;
            GoogleSignIn.Configuration.RequestIdToken = true;
            Logger.Log(LogLevel.Verbose, "Calling SignIn");

            var task = GoogleSignIn.DefaultInstance.SignIn();
            await task;
            FinishSignIn(task);
        }

        void FinishSignIn(Task<GoogleSignInUser> task)
        {
            if (task.IsFaulted)
            {
                using (IEnumerator<Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator())
                {
                    if (enumerator.MoveNext())
                    {
                        GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
                        Logger.Log(LogLevel.Verbose, "Error: " + error.Status + " " + error.Message);
                    }
                    else
                    {
                        Logger.Log(LogLevel.Verbose, task.Exception.Message);
                    }
                }
            }
            else if (task.IsCanceled)
            {
                Logger.Log(LogLevel.Verbose, "Google Sign In task was Canceled");
            }
            else
            {
                // --- This is the important line to include in your own implementation ---
                ModioPlatformGoogleSignIn.SetAsPlatform(task.Result.IdToken);
                Logger.Log(LogLevel.Verbose, "Sign in completed!");
            }
        }

        public void SignOut()
        {
            if (Application.isEditor)
                return;

            Logger.Log(LogLevel.Verbose, "Calling SignOut");
            GoogleSignIn.DefaultInstance.SignOut();
        }

        public void Disconnect()
        {
            if (Application.isEditor)
                return;

            Logger.Log(LogLevel.Verbose, "Calling Disconnect");
            GoogleSignIn.DefaultInstance.Disconnect();
        }
#endif
    }
}
