using System;
using Modio.API;
using UnityEngine;
using UnityEngine.UI;
using UserProfile = Modio.Users.UserProfile;

namespace Modio.Unity.UI.Components.UserProperties
{
    [Serializable]
    public class UserPropertyPlatformName : IUserProperty
    {
        [SerializeField] GameObject[] _enableIsUsernameDefinedByPortal;

        [SerializeField] Image _platformImage;

        [SerializeField] PlatformIcon[] _platformIcons;

        [Serializable]
        class PlatformIcon
        {
            public ModioAPI.Portal Portal;
            public Sprite Icon;
        }

        public void OnUserUpdate(UserProfile user)
        {
            var isUsernameDefinedByPortal = user != null && !string.IsNullOrEmpty(user.PortalUsername);

            foreach (GameObject gameObject in _enableIsUsernameDefinedByPortal)
            {
                gameObject.SetActive(isUsernameDefinedByPortal);
            }

            if (_platformImage != null && isUsernameDefinedByPortal)
            {
                Sprite platformIcon = null;
                
                ModioAPI.Portal currentPortal = ModioAPI.CurrentPortal;

                foreach (PlatformIcon iconPair in _platformIcons)
                {
                    if (iconPair.Portal == currentPortal)
                    {
                        platformIcon = iconPair.Icon;
                    }
                }

                _platformImage.enabled = platformIcon != null;
                _platformImage.sprite = platformIcon;
            }
        }
    }
}
