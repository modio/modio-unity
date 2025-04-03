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
            ModioPanelManager.GetPanelOfType<ModioAuthenticationPanel>().AttemptSso(true).ForgetTaskSafely();
        }
    }
}
