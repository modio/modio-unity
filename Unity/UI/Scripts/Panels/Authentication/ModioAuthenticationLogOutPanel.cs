using Modio.Users;

namespace Modio.Unity.UI.Panels.Authentication
{
    public class ModioAuthenticationLogOutPanel : ModioPanelBase
    {
        public void OnPressLogout()
        {
            User.LogOut();
            ClosePanel();
        }
    }
}
