using Modio.Mods;
using Modio.Unity.UI.Components;

namespace Modio.Unity.UI.Panels.Report
{
    public class ModioReportPanel : ModioPanelBase
    {
        ModioUIMod _modioUIMod;

        protected override void Awake()
        {
            base.Awake();
            _modioUIMod = GetComponent<ModioUIMod>();
        }

        public void OpenReportFlow(Mod mod)
        {
            _modioUIMod.SetMod(mod);

            ModioPanelManager.GetPanelOfType<ModioReportTypePanel>()?.OpenPanel();
        }

        void LateUpdate()
        {
            if (HasFocus) ClosePanel();
        }
    }
}
