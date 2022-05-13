using ModIO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser
{
    /// <summary>
    ///the main interface for interacting with the Mod Browser UI
    /// </summary>
    public partial class Browser
    {
	    [Header("Report Panel")]
	    [SerializeField] GameObject ReportPanel;
	    [SerializeField] TMP_Text ReportPanelHeader;
	    [SerializeField] TMP_Text ReportPanelSubHeader;
	    [SerializeField] TMP_Text ReportPanelSubSubHeader;
	    [SerializeField] TMP_Text ReportPanelText;
	    [SerializeField] TMP_Text ReportPanelCaption;
	    [SerializeField] GameObject ReportPanelReportOptions;
	    [SerializeField] Button ReportPanelFirstReportOption;
	    [SerializeField] GameObject ReportPanelEmailSection;
	    [SerializeField] TMP_InputField ReportPanelEmailField;
	    [SerializeField] GameObject ReportPanelDetailsSection;
	    [SerializeField] TMP_InputField ReportPanelDetailsField;
	    [SerializeField] GameObject ReportPanelSummary;
	    [SerializeField] TMP_Text ReportPanelSummaryReason;
	    [SerializeField] TMP_Text ReportPanelSummaryEmail;
	    [SerializeField] TMP_Text ReportPanelSummaryDetails;
	    [SerializeField] GameObject ReportPanelButtons;
	    [SerializeField] Button ReportPanelBackButton;
	    [SerializeField] Button ReportPanelCancelButton;
	    [SerializeField] Button ReportPanelSubmitButton;
	    [SerializeField] Button ReportPanelNextButton;
	    [SerializeField] Button ReportPanelDoneButton;
	    [SerializeField] GameObject ReportPanelLoadingAnimation;

	    Selectable defaultSelectableOnReportClose;
	    ModProfile modBeingReported;
	    ReportType reportType;
	    
#region Report Panel States

	    public void CloseReportPanel()
	    {
		    ReportPanel.SetActive(false);
		    defaultSelectableOnReportClose?.Select();
	    }
	    
        public void OpenReportPanel(ModProfile modToReport, Selectable selectableOnClose)
        {
	        defaultSelectableOnReportClose = selectableOnClose;
	        currentFocusedPanel = ReportPanel;
	        modBeingReported = modToReport;
	        
	        HideReportPanelObjects();
	        ReportPanel.SetActive(true);
	        ReportPanelReportOptions.SetActive(true);
	        ReportPanelText.gameObject.SetActive(true);

	        ReportPanelHeader.text = "Report a problem";
	        ReportPanelSubHeader.text = $"'{modBeingReported.name}'";
	        ReportPanelText.text = "Report content violating the sites Terms of Use or submit a "
	                               + "DMCA complaint using the form below. Make sure you include "
	                               + "all relevant information and links. If you’d like to report "
	                               + "Copyright Infringement and are the copyright holder, select "
	                               + "‘DMCA’ below.";

	        ReportPanelFirstReportOption.Select();
        }

        public void OpenReportPanel_Email()
        {
	        HideReportPanelObjects();
	        ReportPanel.SetActive(true);
	        ReportPanelText.gameObject.SetActive(true);
	        ReportPanelText.text = "Your email may be shared with moderators and the person that "
	                               + "posted the allegedly infringing content you are reporting.";
	        
	        ReportPanelEmailSection.SetActive(true);
	        
	        ReportPanelButtons.SetActive(true);
	        ReportPanelBackButton.gameObject.SetActive(true);
	        
	        ReportPanelBackButton.gameObject.SetActive(true);
	        ReportPanelBackButton.onClick.RemoveAllListeners();
	        ReportPanelBackButton.onClick.AddListener(delegate { OpenReportPanel(modBeingReported, defaultSelectableOnReportClose); });
	        
	        ReportPanelNextButton.gameObject.SetActive(true);
	        ReportPanelCancelButton.gameObject.SetActive(true);
	        
	        ReportPanelEmailField.Select();
        }

        public void OpenReportPanel_Details()
        {
	        HideReportPanelObjects();
	        ReportPanel.SetActive(true);
	        
	        ReportPanelCaption.gameObject.SetActive(true);
	        ReportPanelCaption.text = "Details of infringement";
	        
	        ReportPanelText.gameObject.SetActive(true);
	        ReportPanelText.text = "To help us process your report, please provide as much detail "
	                               + "and evidence as possible.";

	        ReportPanelDetailsSection.SetActive(true);
	        ReportPanelDetailsField.text = "";
	        
	        ReportPanelButtons.SetActive(true);
	        ReportPanelBackButton.gameObject.SetActive(true);
	        
	        ReportPanelBackButton.gameObject.SetActive(true);
	        ReportPanelBackButton.onClick.RemoveAllListeners();
	        ReportPanelBackButton.onClick.AddListener(OpenReportPanel_Email);
	        
	        ReportPanelSubmitButton.gameObject.SetActive(true);
	        ReportPanelSubmitButton.onClick.RemoveAllListeners();
	        ReportPanelSubmitButton.onClick.AddListener(OpenReportPanel_Summary);
	        
	        ReportPanelCancelButton.gameObject.SetActive(true);
	        
	        ReportPanelDetailsField.Select();
        }

        public void OpenReportPanel_Summary()
        {
	        HideReportPanelObjects();
	        ReportPanel.SetActive(true);
	        
	        ReportPanelSummary.SetActive(true);

	        ReportPanelSummaryEmail.gameObject.SetActive(true);
	        ReportPanelSummaryEmail.text = ReportPanelEmailField.text;
	        
	        ReportPanelSummaryReason.gameObject.SetActive(true);
	        ReportPanelSummaryReason.text = reportType.ToString();
	        
	        ReportPanelSummaryDetails.gameObject.SetActive(true);
	        ReportPanelSummaryDetails.text = ReportPanelDetailsField.text;
	        
	        ReportPanelButtons.SetActive(true);
	        
	        ReportPanelBackButton.gameObject.SetActive(true);
	        ReportPanelBackButton.onClick.RemoveAllListeners();
	        ReportPanelBackButton.onClick.AddListener(OpenReportPanel_Details);
	        
	        ReportPanelSubmitButton.gameObject.SetActive(true);
	        ReportPanelSubmitButton.onClick.RemoveAllListeners();
	        ReportPanelSubmitButton.onClick.AddListener(SendReport);
	        
	        ReportPanelCancelButton.gameObject.SetActive(true);
	        
	        ReportPanelSubmitButton.Select();
        }

        public void OpenReportPanel_Done()
        {
	        HideReportPanelObjects();
	        ReportPanel.SetActive(true);
	        
	        ReportPanelText.gameObject.SetActive(true);
	        ReportPanelText.text = "The mod has been reported. A confirmation email will be sent "
	                               + "to you shortly with the details and the moderators of the mod"
	                               + " will be notified.";
	        
	        ReportPanelButtons.SetActive(true);
	        ReportPanelDoneButton.gameObject.SetActive(true);
	        
	        ReportPanelDoneButton.Select();
        }

        public void OpenReportPanel_Waiting()
        {
	        HideReportPanelObjects();
	        ReportPanel.SetActive(true);
	        
	        ReportPanelButtons.SetActive(true);
	        ReportPanelLoadingAnimation.SetActive(true);
        }

        public void OpenReportPanel_Problem()
        {
	        HideReportPanelObjects();
	        ReportPanel.SetActive(true);
	        
	        ReportPanelText.gameObject.SetActive(true);
	        ReportPanelText.text = "Something went wrong when trying to send your report.";
	        
	        ReportPanelButtons.SetActive(true);
	        ReportPanelCancelButton.gameObject.SetActive(true);
        }

        public void SetReportType(int type)
        {
	        reportType = (ReportType)type;
        }

        public void HideReportPanelObjects()
        {
	        ReportPanelEmailSection.SetActive(false);
	        ReportPanelSubSubHeader.gameObject.SetActive(false);
	        ReportPanelText.gameObject.SetActive(false);
	        ReportPanelCaption.gameObject.SetActive(false);
	        ReportPanelReportOptions.SetActive(false);
	        ReportPanelDetailsSection.SetActive(false);
	        ReportPanelSummary.SetActive(false);
	        ReportPanelButtons.SetActive(false);
	        ReportPanelBackButton.gameObject.SetActive(false);
	        ReportPanelNextButton.gameObject.SetActive(false);
	        ReportPanelCancelButton.gameObject.SetActive(false);
	        ReportPanelDoneButton.gameObject.SetActive(false);
	        ReportPanelSubmitButton.gameObject.SetActive(false);
	        ReportPanelLoadingAnimation.SetActive(false);
        }

#endregion //  Report Panel States
	    
#region Send / Receive responses

	    public void SendReport()
	    {
		    OpenReportPanel_Waiting();
			    
		    Report report = new Report(modBeingReported.id, reportType, ReportPanelDetailsField.text, "Unknown", ReportPanelEmailField.text);
		    ModIOUnity.Report(report, ReportSent);
	    }

	    public void ReportSent(Result result)
	    {
		    if(result.Succeeded())
		    {
			    OpenReportPanel_Done();
		    }
		    else
		    {
			    OpenReportPanel_Problem();
		    }
	    }
	    
#endregion

    }
}
