using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ModIO.Implementation.API.Objects;

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

#endregion // Fields

        /// <summary>Convenience wrapper for determining if a valid token is in use.</summary>
        public bool IsOAuthTokenValid()
        {
            return (!string.IsNullOrEmpty(oAuthToken) && !oAuthTokenWasRejected);
        }

        public async Task SetUserObject(UserObject user)
        {
            userObject = user;
            ModCollectionManager.AddUserToRegistry(user);
            await DataStorage.SaveUserData();
        }

        public async Task ClearUser()
        {
            userObject = default;
            ClearAuthenticatedSession();
            await DataStorage.SaveUserData();
        }

        /// <summary>Convenience wrapper that sets OAuthToken and clears rejected flag.</summary>
        public async Task SetOAuthToken(AccessTokenObject newToken)
        {
            oAuthToken = newToken.access_token;
            oAuthExpiryDate = newToken.date_expires;
            oAuthTokenWasRejected = false;
            await DataStorage.SaveUserData();
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
