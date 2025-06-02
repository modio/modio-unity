using System;
using System.Threading.Tasks;
using Modio.Mods;
using Modio.Unity.UI.Panels;
using Modio.Unity.UI.Panels.Report;
using Modio.Unity.UI.Search;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyOptionButtonsForPanels : IModProperty
    {
        [SerializeField] Button _viewModButton;
        [SerializeField] Button _moreFromCreatorButton;
        [SerializeField] Button _reportModButton;
        [SerializeField] Button _retryDownloadButton;
        [SerializeField] Button _uninstallButton;

        Mod _mod;

        public void OnModUpdate(Mod mod)
        {
            _mod = mod;

            SetupButton(_viewModButton,         ViewModButtonClicked);
            SetupButton(_moreFromCreatorButton, MoreFromCreatorButtonClicked);
            SetupButton(_reportModButton,       ReportModButtonClicked);
            SetupButton(_retryDownloadButton,   RetryDownloadButtonClicked);
            SetupButton(_uninstallButton,       UninstallModButtonClicked);

            return;

            void SetupButton(Button button, UnityAction listener)
            {
                if (button == null) return;
                button.onClick.RemoveListener(listener);
                button.onClick.AddListener(listener);
            }
        }

        void ViewModButtonClicked()
        {
            ModioPanelManager.GetPanelOfType<ModDisplayPanel>().OpenPanel(_mod);
        }

        void MoreFromCreatorButtonClicked()
        {
            ModioUISearch.Default.SetSearchForUser(_mod.Creator);
        }

        void ReportModButtonClicked()
        {
            ModioPanelManager.GetPanelOfType<ModioReportPanel>().OpenReportFlow(_mod);
        }

        void RetryDownloadButtonClicked()
        {
            Task<Error> retryInstallingModTask = ModInstallationManagement.RetryInstallingMod(_mod);

            ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()
                             ?.MonitorTaskThenOpenPanelIfError(retryInstallingModTask);
        }

        void UninstallModButtonClicked()
        {
            _mod.UninstallOtherUserMod();
        }
    }
}
