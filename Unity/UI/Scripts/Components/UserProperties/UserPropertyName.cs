using System;
using Modio.Unity.UI.Components.Localization;
using Modio.Users;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.UserProperties
{
    [Serializable]
    public class UserPropertyName : IUserProperty
    {
        [SerializeField] TMP_Text _text;
        [SerializeField] ModioUILocalizedText _localisedText;
        [SerializeField] string _userLoggedInFormat = "{0}";
        [SerializeField] string _noUserLoggedIn;

        public void OnUserUpdate(UserProfile user)
        {
            if (user?.Username != null)
            {
                string nameToUse = string.IsNullOrEmpty(user.PortalUsername) ? user.Username : user.PortalUsername;
                
                if (_localisedText != null)
                {
                    _localisedText.SetFormatArgs(nameToUse);

                    return;
                }

                if (!string.IsNullOrEmpty(_userLoggedInFormat)) nameToUse = string.Format(_userLoggedInFormat, nameToUse);
                _text.text = nameToUse;
            }
            else
            {
                if (_localisedText != null)
                {
                    _localisedText.SetFormatArgs("");

                    return;
                }

                if (_text != null) _text.text = _noUserLoggedIn;
            }
        }
    }
}
