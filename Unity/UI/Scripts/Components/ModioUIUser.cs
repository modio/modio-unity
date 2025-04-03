using Modio.Users;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Components
{
    public class ModioUIUser : MonoBehaviour, IModioUIPropertiesOwner
    {
        public UnityEvent onUserUpdate;

        [SerializeField] bool _useLoggedInUser;

        public UserProfile User { get; private set; }

        void Start()
        {
            if (_useLoggedInUser)
            {
                Modio.Users.User.OnUserChanged += OnUserChanged;

                GetCurrentUser();
            }
        }

        void OnDestroy()
        {
            if (User != null) User.OnProfileUpdated -= ProfileUpdated;

            Modio.Users.User.OnUserChanged -= OnUserChanged;
        }

        public void AddUpdatePropertiesListener(UnityAction listener) => onUserUpdate.AddListener(listener);

        public void RemoveUpdatePropertiesListener(UnityAction listener) => onUserUpdate.RemoveListener(listener);

        void GetCurrentUser() => SetUser(Modio.Users.User.Current?.Profile);

        void OnUserChanged(User user) => SetUser(user.Profile);

        public void SetUser(UserProfile profile)
        {
            if (User != null) User.OnProfileUpdated -= ProfileUpdated;

            User = profile;

            if (profile != null) User.OnProfileUpdated += ProfileUpdated;

            ProfileUpdated();
        }

        void ProfileUpdated() => onUserUpdate.Invoke();
    }
}
