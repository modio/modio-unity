using Modio.Authentication;
using Modio.Extensions;

namespace Modio.Unity.UI.Panels.Authentication
{
    public class ModioAuthenticationTermsOfServicePanel : ModioPanelBase
    {
        /// <summary>
        /// Handles signing on via SSO when the user presses the Agree to TOS button.
        /// </summary>
        public void OnPressAgreeTOS()
        {
            var service = ModioServices.Resolve<IModioAuthService>();

            ModioPanelManager.GetPanelOfType<ModioAuthenticationPanel>().AttemptSso(service, true).ForgetTaskSafely();
        }
    }
}
