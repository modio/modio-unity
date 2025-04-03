using System;
using Modio.Users;
using UnityEngine;

namespace Modio.Unity.UI.Components.UserProperties
{
    [Serializable]
    public class UserPropertyEnableOnLogin : IUserProperty
    {
        [SerializeField] GameObject[] _activeWhenLoggedOut;
        [SerializeField] GameObject[] _activeWhenLoggedIn;

        public void OnUserUpdate(UserProfile user)
        {
            bool userLoggedIn = user != null;

            foreach (GameObject go in _activeWhenLoggedOut)
            {
                go.SetActive(!userLoggedIn);
            }

            foreach (GameObject go in _activeWhenLoggedIn)
            {
                go.SetActive(userLoggedIn);
            }
        }
    }
}
