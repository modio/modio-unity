using System;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Search;
using Modio.Users;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.UserProperties
{
    [Serializable]
    public class UserPropertySeeMoreFromUserButton : IUserProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] Button _button;
        UserProfile _user;

        public void OnUserUpdate(UserProfile user)
        {
            _user = user;
        }

        public void Start() { }

        public void OnDestroy() { }

        public void OnEnable()
        {
            _button.onClick.AddListener(OnClicked);
        }

        public void OnDisable()
        {
            _button.onClick.RemoveListener(OnClicked);
        }

        void OnClicked()
        {
            if (_user != null)
            {
                ModioUISearch.Default.SetSearchForUser(_user);

                ModioPanelBase currentFocusedPanel = ModioPanelManager.GetInstance().CurrentFocusedPanel;

                if (currentFocusedPanel is ModDisplayPanel or ModCollectionDisplayPanel)
                    currentFocusedPanel.ClosePanel();
            }
        }
    }
}
