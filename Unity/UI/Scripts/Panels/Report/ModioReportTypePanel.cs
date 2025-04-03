using Modio.Reports;

namespace Modio.Unity.UI.Panels.Report
{
    public class ModioReportTypePanel : ModioPanelBase
    {
        public void OnUserSubmittedReportType(int type)
        {
            OnUserSubmittedReportTypeEnum((ReportType)type);
        }

        public void OnUserSubmittedReportTypeEnum(ReportType type)
        {
            ClosePanel();
            ModioPanelManager.GetPanelOfType<ModioReportDetailsPanel>().OpenPanel(type);
        }
    }
}
