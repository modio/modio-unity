using System;
using Modio.Errors;
using Modio.Mods;
using Modio.Unity.UI.Components.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class ModPropertyFileOperations : IModProperty
    {
        [Flags]
        enum Operation
        {
            None                 = 0b00000000,
            Queued               = 0b00000001,
            Downloading          = 0b00000010,
            Installing           = 0b00000100,
            Installed            = 0b00001000,
            Updating             = 0b00010000,
            Uninstalling         = 0b00100000,
            FileOperationFailed  = 0b01000000,
            InstalledByOtherUser = 0b10000000,
        }

        [SerializeField] Operation _operations = ~Operation.Installed;
        [Space][SerializeField] GameObject _noOperationActive;
        [SerializeField] GameObject _operationActive;
        [Space][SerializeField] TMP_Text _operationName;
        [SerializeField] ModioUILocalizedText _operationNameLocalised;
        [SerializeField] TMP_Text _progressPercent;
        [SerializeField] Image _progressFill;
        [SerializeField] bool _invertProgressFill;
        [SerializeField] TMP_Text _downloadSpeed;

        public void OnModUpdate(Mod mod)
        {
            Operation operation = mod.File.State switch
            {
                ModFileState.Queued              => Operation.Queued,
                ModFileState.Downloading         => Operation.Downloading,
                ModFileState.Installing          => Operation.Installing,
                ModFileState.Installed           => mod.IsSubscribed 
                    ? Operation.Installed 
                    : Operation.InstalledByOtherUser,
                ModFileState.Updating            => Operation.Updating,
                ModFileState.Uninstalling        => Operation.Uninstalling,
                ModFileState.FileOperationFailed => Operation.FileOperationFailed,
                _                                => Operation.None,
            };

            bool active = operation != Operation.None && _operations.HasFlag(operation);

            if (_noOperationActive != null) _noOperationActive.gameObject.SetActive(!active);
            if (_operationActive != null) _operationActive.gameObject.SetActive(active);

            if (!active) return;

            if (_operationName != null) _operationName.text = operation.ToString();

            if (_operationNameLocalised != null)
            {
                var locKey = operation switch
                {
                    Operation.None                => "",
                    Operation.Queued              => ModioUILocalizationKeys.Modstate_Queued,
                    Operation.Downloading         => ModioUILocalizationKeys.Modstate_Downloading,
                    Operation.Installing          => ModioUILocalizationKeys.Modstate_Installing,
                    Operation.Installed           => ModioUILocalizationKeys.Modstate_Installed,
                    Operation.Updating            => ModioUILocalizationKeys.Modstate_Updating,
                    Operation.Uninstalling        => ModioUILocalizationKeys.Modstate_Uninstalling,
                    Operation.FileOperationFailed => mod.File.FileStateErrorCause.Code == ErrorCode.INSUFFICIENT_SPACE
                                                        ? ModioUILocalizationKeys.Modstate_Error_Storage
                                                        : ModioUILocalizationKeys.Modstate_Error,
                    Operation.InstalledByOtherUser => ModioUILocalizationKeys.Modstate_Installed,
                    _                              => throw new ArgumentOutOfRangeException(),
                };

                _operationNameLocalised.SetKey(locKey);
            }

            if (_progressPercent != null)
                _progressPercent.text = mod.File.FileStateProgress.ToString(
                    "P0",
                    ModioUILocalizationManager.CultureInfo
                );

            if (_progressFill != null)
                _progressFill.fillAmount =
                    _invertProgressFill ? 1 - mod.File.FileStateProgress : mod.File.FileStateProgress;

            if (_downloadSpeed != null)
            {
                bool downloading = operation == Operation.Downloading;

                if (downloading)
                    _downloadSpeed.text = $"{StringFormat.BytesSuffix(mod.File.DownloadingBytesPerSecond, true)}/s";
                _downloadSpeed.gameObject.SetActive(downloading);
            }
        }
    }
}
