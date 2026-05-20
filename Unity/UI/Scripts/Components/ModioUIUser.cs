using Modio.Users;
using UnityEngine;
using UnityEngine.Events;

namespace Modio.Unity.UI.Components
{
    /// <summary>
    /// A UI container for a <see cref="UserProfile"/>. Assign a user to this, and any
    /// child <see cref="ModioUIUserProperties"/> will get updates about the user
    /// </summary>
    /// <remarks>This will subscribe to the <see cref="UserProfile"/>'s `OnProfileUpdated` event and post updates to all child properties automatically.</remarks>
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
