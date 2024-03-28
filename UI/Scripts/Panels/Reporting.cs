using ModIO;
using ModIO.Util;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{
    public class Reporting : SelfInstancingMonoSingleton<Reporting>
    {
        private const int REQUIRED_DESCRIPTION_LENGTH = 20;

        [SerializeField] public GameObject Panel;
        [SerializeField] TMP_Text ReportPanelHeader;
        [SerializeField] TMP_Text ReportPanelSubHeader;
        [SerializeField] TMP_Text ReportPanelSubSubHeader;
        [SerializeField] TMP_Text ReportPanelText;
        [SerializeField] TMP_Text ReportPanelCaption;
        [SerializeField] GameObject ReportPanelReportOptions;
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

        [SerializeField] TMP_Text ReportPanelInvalidEmail;
        [SerializeField] TMP_Text ReportPanelInvalidDescription;

        internal Translation ReportPanelHeaderTranslation = null;
        internal Translation ReportPanelTextTranslation = null;
        internal Translation ReportPanelCaptionTranslation = null;


        Selectable defaultSelectableOnReportClose;
        ModProfile modBeingReported;
        ReportType reportType;

        #region Report Panel States

        public void Close()
        {
            Panel.SetActive(false);
            InputNavigation.Instance.Select(defaultSelectableOnReportClose);
        }

        public void Open(ModProfile modToReport, Selectable selectableOnClose)
        {
            defaultSelectableOnReportClose = selectableOnClose;
            Browser.currentFocusedPanel = Panel;
            modBeingReported = modToReport;

            HideReportPanelObjects();
            Panel.SetActive(true);
            ReportPanelReportOptions.SetActive(true);
            ReportPanelText.gameObject.SetActive(true);

            Translation.Get(ReportPanelHeaderTranslation, "Report a problem", ReportPanelHeader);
            ReportPanelSubHeader.text = $"'{modBeingReported.name}'";
            Translation.Get(ReportPanelTextTranslation, "Report content violating the sites Terms of Use or submit a DMCA complaint using the form below. Make sure you include all relevant information and links. If you’d like to report Copyright Infringement and are the Copyright holder, select ‘DMCA’ below.", ReportPanelText);

            SelectionManager.Instance.SelectView(UiViews.Report);

            ReportPanelButtons.SetActive(true);
            ReportPanelCancelButton.gameObject.SetActive(true);
        }

        public void OpenEmail()
        {
            HideReportPanelObjects();
            Panel.SetActive(true);
            ReportPanelText.gameObject.SetActive(true);
            Translation.Get(ReportPanelTextTranslation, "Your email may be shared with moderators and the person that posted the allegedly infringing content you are reporting.", ReportPanelText);

            ReportPanelEmailSection.SetActive(true);

            ReportPanelButtons.SetActive(true);
            ReportPanelBackButton.gameObject.SetActive(true);

            ReportPanelBackButton.gameObject.SetActive(true);
            ReportPanelBackButton.onClick.RemoveAllListeners();
            ReportPanelBackButton.onClick.AddListener(delegate
            { Open(modBeingReported, defaultSelectableOnReportClose); });

            ReportPanelNextButton.gameObject.SetActive(true);
            ReportPanelCancelButton.gameObject.SetActive(true);

            InputNavigation.Instance.Select(ReportPanelEmailField);
        }

        static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // checks (something@something.), doesn't cover more extreme edge cases.
            string pattern = @"^\S+@\S+\.\S+$";

            return Regex.IsMatch(email, pattern);
        }

        public void CheckValidEmailAndOpenDetails()
        {
            var isValid = IsValidEmail(ReportPanelEmailField.text);

            ReportPanelInvalidEmail.gameObject.SetActive(!isValid);

            if (isValid)
            {
                OpenDetails();
            }
        }

        static bool IsValidDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return false;
            }

            return description.TrimEnd().Length >= REQUIRED_DESCRIPTION_LENGTH;
        }

        public void CheckValidDescriptionsAndOpenSummary()
        {
            var isValid = IsValidDescription(ReportPanelDetailsField.text);

            ReportPanelInvalidDescription.gameObject.SetActive(!isValid);

            if (isValid)
            {
                OpenSummary();
            }
        }

        public void OpenDetails()
        {
            HideReportPanelObjects();
            Panel.SetActive(true);

            ReportPanelCaption.gameObject.SetActive(true);
            Translation.Get(ReportPanelCaptionTranslation, "Details of infringement", ReportPanelCaption);

            ReportPanelText.gameObject.SetActive(true);
            Translation.Get(ReportPanelTextTranslation, "To help us process your report, please provide as much detail and evidence as possible.", ReportPanelText);

            ReportPanelDetailsSection.SetActive(true);
            ReportPanelDetailsField.text = "";

            ReportPanelButtons.SetActive(true);
            ReportPanelBackButton.gameObject.SetActive(true);

            ReportPanelBackButton.gameObject.SetActive(true);
            ReportPanelBackButton.onClick.RemoveAllListeners();
            ReportPanelBackButton.onClick.AddListener(OpenEmail);

            ReportPanelSubmitButton.gameObject.SetActive(true);
            ReportPanelSubmitButton.onClick.RemoveAllListeners();
            ReportPanelSubmitButton.onClick.AddListener(CheckValidDescriptionsAndOpenSummary);

            ReportPanelCancelButton.gameObject.SetActive(true);

            InputNavigation.Instance.Select(ReportPanelDetailsField);
        }

        public void OpenSummary()
        {
            HideReportPanelObjects();
            Panel.SetActive(true);

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
            ReportPanelBackButton.onClick.AddListener(OpenDetails);

            ReportPanelSubmitButton.gameObject.SetActive(true);
            ReportPanelSubmitButton.onClick.RemoveAllListeners();
            ReportPanelSubmitButton.onClick.AddListener(Send);

            ReportPanelCancelButton.gameObject.SetActive(true);

            InputNavigation.Instance.Select(ReportPanelSubmitButton);
        }

        public void OpenDone()
        {
            HideReportPanelObjects();
            Panel.SetActive(true);

            ReportPanelText.gameObject.SetActive(true);
            Translation.Get(ReportPanelTextTranslation, "The mod has been reported. A confirmation email will be sent to you shortly with the details and the moderators of the mod will be notified.", ReportPanelText);

            ReportPanelButtons.SetActive(true);
            ReportPanelDoneButton.gameObject.SetActive(true);

            InputNavigation.Instance.Select(ReportPanelDoneButton);
        }

        public void OpenWaiting()
        {
            HideReportPanelObjects();
            Panel.SetActive(true);

            ReportPanelButtons.SetActive(true);
            ReportPanelLoadingAnimation.SetActive(true);
        }

        public void OpenProblem()
        {
            HideReportPanelObjects();
            Panel.SetActive(true);

            ReportPanelText.gameObject.SetActive(true);

            TextAlignmentOptions alignment = ReportPanelText.alignment;
            alignment = TextAlignmentOptions.Center;
            ReportPanelText.alignment = alignment;

            Translation.Get(ReportPanelTextTranslation, "Something went wrong when trying to send your report.", ReportPanelText);

            ReportPanelButtons.SetActive(true);
            ReportPanelCancelButton.gameObject.SetActive(true);
        }

        public void SetReportType(int type)
        {
            reportType = (ReportType)type;
        }

        public void HideReportPanelObjects()
        {
            TextAlignmentOptions alignment = ReportPanelText.alignment;
            alignment = TextAlignmentOptions.Left;
            ReportPanelText.alignment = alignment;
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

        public void Send()
        {
            OpenWaiting();

            Report report = new Report(modBeingReported.id, reportType, ReportPanelDetailsField.text, "Unknown", ReportPanelEmailField.text);
            ModIOUnity.Report(report, Sent);
        }

        private void Sent(Result result)
        {
            if (result.Succeeded())
            {
                OpenDone();
            }
            else
            {
                OpenProblem();
            }
        }
    }
}
