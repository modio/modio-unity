using System;
using Modio.Users;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Modio.Unity.UI.Components.UserProperties
{
    [Serializable, MovedFrom(true, "Modio.Unity.UI.Components.UserProperties", null, "ModPropertyWalletAmount")]
    public class UserPropertyWalletAmount : IUserProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] TMP_Text _text;

        bool hasSetText = false;

        public void OnUserUpdate(UserProfile user)
        {
            Wallet wallet = User.Current?.Wallet;
            
            _text.text = wallet != null ? (wallet.Balance).ToString() : "";
            hasSetText = true;
        }

        public void Start() { }

        public void OnDestroy() { }

        public void OnEnable()
        {
            if (!hasSetText) _text.text = "";
        }

        public void OnDisable() { }
    }
}
