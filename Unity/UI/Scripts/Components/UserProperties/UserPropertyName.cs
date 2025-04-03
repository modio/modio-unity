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
                if (_localisedText != null)
                {
                    _localisedText.SetFormatArgs(user.Username);

                    return;
                }

                var username = user.Username;
                if (!string.IsNullOrEmpty(_userLoggedInFormat)) username = string.Format(_userLoggedInFormat, username);
                _text.text = username;
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
