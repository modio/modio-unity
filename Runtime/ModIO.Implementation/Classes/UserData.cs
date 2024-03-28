using System;
using System.Collections.Generic;
using ModIO.Implementation.API;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.Wss.Messages.Objects;

namespace ModIO.Implementation
{
    /// <summary>Stores the user data for the session.</summary>
    [Serializable]
    internal class UserData
    {
        /// <summary>Variable backing for the singleton.</summary>
        public static UserData instance = null;

#region Fields

        // TODO @Jackson Replace this with AccessToken.cs Type (so it has dateExpires and provider)
        /// <summary>OAuthToken assigned to the user.</summary>
        public string oAuthToken;

        public AuthenticationServiceProvider currentServiceProvider = AuthenticationServiceProvider.None;

        public long oAuthExpiryDate;

        /// <summary>Has the token been rejected.</summary>
        public bool oAuthTokenWasRejected;

        // TODO Consolidate this with Registry
        public Dictionary<ModId, SubscribedMod> queuedUnsubscribedMods =
            new Dictionary<ModId, SubscribedMod>();

        /// <summary>
        /// Assigned on Authentication methods.
        /// </summary>
        public UserObject userObject;

        public string rootLocalStoragePath;

#endregion // Fields

        /// <summary>Convenience wrapper for determining if a valid token is in use.</summary>
        public bool IsOAuthTokenValid()
        {
            return (!string.IsNullOrEmpty(oAuthToken) && !oAuthTokenWasRejected);
        }

        public void SetUserObject(UserObject user)
        {
            userObject = user;
            ModCollectionManager.AddUserToRegistry(user);
            DataStorage.SaveUserData();
        }

        public void ClearUser()
        {
            userObject = default;
            ClearAuthenticatedSession();
            DataStorage.SaveUserData();
        }

        /// <summary>Convenience wrapper that sets OAuthToken and clears rejected flag.</summary>
        public void SetOAuthToken(AccessTokenObject newToken, AuthenticationServiceProvider serviceProvider)
        {
            ResponseCache.ClearCache();
            currentServiceProvider = serviceProvider;
            oAuthToken = newToken.access_token;
            oAuthExpiryDate = newToken.date_expires;
            oAuthTokenWasRejected = false;
            DataStorage.SaveUserData();
        }

        /// <summary>Convenience wrapper that sets OAuthToken and clears rejected flag.</summary>
        public void SetOAuthToken(WssLoginSuccess newToken)
        {
            oAuthToken = newToken.access_token;
            oAuthExpiryDate = newToken.date_expires;
            oAuthTokenWasRejected = false;
            DataStorage.SaveUserData();
        }

        public void SetOAuthTokenAsRejected()
        {
            oAuthTokenWasRejected = true;
        }

        internal void ClearAuthenticatedSession()
        {
            oAuthToken = default;
            oAuthTokenWasRejected = false;
        }
    }
}
