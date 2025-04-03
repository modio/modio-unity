using Modio.Users;

namespace Modio.Unity.UI.Components.UserProperties
{
    public interface IUserProperty
    {
        void OnUserUpdate(UserProfile user);
    }
}
