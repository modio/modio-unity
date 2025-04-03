using Modio.Users;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components.UserProperties
{
    public abstract class UserPropertyNumberBase : IUserProperty
    {
        [SerializeField] TMP_Text _text;
        [SerializeField, Tooltip(StringFormat.KILO_FORMAT_TOOLTIP)]
        StringFormatKilo _format = StringFormatKilo.Kilo;
        [SerializeField, ShowIf(nameof(IsCustomFormat))]
        string _customFormat;

        public void OnUserUpdate(UserProfile user) => _text.text = StringFormat.Kilo(_format, GetValue(user), _customFormat);

        protected abstract long GetValue(UserProfile user);

        bool IsCustomFormat() => _format == StringFormatKilo.Custom;
    }
}
