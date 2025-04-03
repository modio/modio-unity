namespace Modio.Unity.UI.Panels
{
    public class MainMenuPanel : ModioPanelBase
    {
        protected override void Start()
        {
            OpenPanel();
            base.Start();
        }

        protected override void CancelPressed()
        {
            // don't call the base method, we don't want to close the main menu
        }
    }
}
