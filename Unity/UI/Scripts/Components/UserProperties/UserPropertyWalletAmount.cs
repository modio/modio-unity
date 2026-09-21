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

        public void OnUserUpdate(UserProfile user)
        {
            // Unfortunately we don't need this user, only User.Current
            // Keeping this an IUserProperty to support our existing implementations 
        }

        public void Start() { }

        public void OnDestroy() { }

        public void OnEnable()
        {
            User.OnUserChanged += OnUserChanged;
            OnUserChanged(User.Current);
        }

        public void OnDisable()
        {
            User.OnUserChanged -= OnUserChanged;
        }

        void OnUserChanged(User user)
        {
            Wallet wallet = user?.Wallet;
            
            _text.text = wallet != null ? (wallet.Balance).ToString() : "";
        }
    }
}
