using System;
using System.Threading.Tasks;
using Modio.Mods;
using Modio.Users;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.UserProperties
{
    [Serializable]
    public class UserPropertyDownloadQueue : IUserProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] Image[] _progressBars;
        [SerializeField] TMP_Text _progressPercentText;
        [SerializeField] TMP_Text _progressSizesText;
        [SerializeField] TMP_Text _operationCountText;
        [SerializeField] TMP_Text _speedText;

        [SerializeField] GameObject _disableIfNoOperations;
        [SerializeField] GameObject _showForDownloadOnly;
        [SerializeField] GameObject _showForInstallOnly;
        [SerializeField] float _hideAfterSecondsOfInactivity = 2;

        int _completedOperationCount;

        Mod _mod;

        public void OnUserUpdate(UserProfile user) { }

        public void Start() { }

        public void OnDestroy() { }

        public void OnEnable()
        {
            if (_disableIfNoOperations != null) _disableIfNoOperations.SetActive(false);

            SetInstallOrDownloadState(false);

            Mod.AddChangeListener(ModChangeType.FileState, OnModChangeEvent);
        }

        public void OnDisable()
        {
            Mod.RemoveChangeListener(ModChangeType.FileState, OnModChangeEvent);
            if (_mod != null) _mod.OnModUpdated -= OnModUpdated;
        }

        void OnModChangeEvent(Mod mod, ModChangeType modChangeType)
        {
            if (mod.File.State is not (ModFileState.Downloading or ModFileState.Installing or ModFileState.Updating or ModFileState.Uninstalling))
            {
                if (_mod != mod) return;
                _completedOperationCount++;
                _mod.OnModUpdated -= OnModUpdated;
                OnModUpdated();
                _mod = null;
                return;
            }
            
            if (_mod != null) _mod.OnModUpdated -= OnModUpdated;
            _mod = mod;
            _mod.OnModUpdated += OnModUpdated;
            
            if (_disableIfNoOperations != null) _disableIfNoOperations.SetActive(true);

            OnModUpdated();
        }

        void OnModUpdated()
        {
            var progressAmount = _mod.File.FileStateProgress;

            var currentOperationCompleted = _mod.File.State == ModFileState.None ||
                                            _mod.File.State == ModFileState.Installed;

            var currentOperationFailed = _mod.File.State == ModFileState.FileOperationFailed;

            if (currentOperationCompleted) progressAmount = 1;

            if (!currentOperationFailed)
            {
                foreach (Image progressBar in _progressBars)
                {
                    progressBar.fillAmount = progressAmount;
                }

                if (_progressPercentText) _progressPercentText.text = $"{progressAmount:P0}";
                if (_progressSizesText)
                {
                    long fileSize = _mod.File.State == ModFileState.Downloading ? _mod.File.ArchiveFileSize : _mod.File.FileSize;

                    string currentDisplayed = StringFormat.Bytes(StringFormatBytes.Suffix, (long)(fileSize * progressAmount), reducePrecision:true);
                    string totalDisplayed = StringFormat.Bytes(StringFormatBytes.Suffix, fileSize, reducePrecision:true);
                    _progressSizesText.text = $"{currentDisplayed} / {totalDisplayed}";
                }
                if (_speedText) _speedText.text = _mod.File.DownloadingBytesPerSecond <= 0 ? string.Empty :
                    "(" + StringFormat.Bytes(StringFormatBytes.Suffix, _mod.File.DownloadingBytesPerSecond, reducePrecision:true) + "/s)";
            }

            int pendingModOperationCount = ModInstallationManagement.PendingModOperationCount;
            
            if (_mod.File.State == ModFileState.Downloading)
                SetInstallOrDownloadState(true);
            else if (_mod.File.State == ModFileState.Installing ||
                     _mod.File.State == ModFileState.Uninstalling ||
                     _mod.File.State == ModFileState.Updating)
                SetInstallOrDownloadState(false);

            if (_operationCountText != null)
            {
                _operationCountText.text = $"{pendingModOperationCount}";
            }

            if ((currentOperationCompleted || currentOperationFailed) && pendingModOperationCount <= 1)
            {
                HideAfterDelay();
            }
        }

        async void HideAfterDelay()
        {
            await Task.Delay((int)(_hideAfterSecondsOfInactivity * 1000));

            var currentOperationFinished = _mod == null ||
                                           _mod.File.State == ModFileState.None ||
                                           _mod.File.State == ModFileState.Installed ||
                                           _mod.File.State == ModFileState.FileOperationFailed;

            if (currentOperationFinished && _disableIfNoOperations != null) _disableIfNoOperations.SetActive(false);
        }

        void SetInstallOrDownloadState(bool isDownloading)
        {
            if (_showForDownloadOnly != null) _showForDownloadOnly.SetActive(isDownloading);
            if (_showForInstallOnly != null) _showForInstallOnly.SetActive(!isDownloading);
        }
    }
}
