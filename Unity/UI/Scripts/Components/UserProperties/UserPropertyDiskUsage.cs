using System;
using System.Threading.Tasks;
using Modio.Mods;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserProfile = Modio.Users.UserProfile;

namespace Modio.Unity.UI.Components.UserProperties
{
    [Serializable]
    public class UserPropertyDiskUsage : IUserProperty, IPropertyMonoBehaviourEvents
    {
        [SerializeField] TMP_Text _text;

        [SerializeField] Image _fillImage;

        [SerializeField] GameObject _enableIfAvailableSpaceSupported;
        [SerializeField] GameObject _disableIfAvailableSpaceSupported;
        bool _isUpdatingUsage;

        public void Start()
        {
            if (_text != null)
                _text.text = "";
        }

        public void OnDestroy() { }

        public void OnEnable()
        {
            ModioClient.OnInitialized += UpdateUsage;
            
            Mod.AddChangeListener(ModChangeType.FileState, OnModFileStateChanged);
        }

        public void OnDisable()
        {
            ModioClient.OnInitialized -= UpdateUsage;
            Mod.RemoveChangeListener(ModChangeType.FileState, OnModFileStateChanged);
        }

        void OnModFileStateChanged(Mod _, ModChangeType __)
        {
            UpdateUsage();
        }

        public void OnUserUpdate(UserProfile user)
        {
            if(!_isUpdatingUsage)
                UpdateUsage();
        }

        async void UpdateUsage()
        {
            _isUpdatingUsage = true;
            
            //wait, so we don't hit this multiple times in one frame
            await Task.Yield();
            
            long usedSpaceBytes = ModInstallationManagement.GetTotalDiskUsage(false);
            long reservedSpaceBytes = ModInstallationManagement.GetTotalDiskUsage(true);
            
            var availableFreeSpaceBytes = await ModioClient.DataStorage.GetAvailableFreeSpaceForModInstall();
            
            bool supportAvailableSpace = availableFreeSpaceBytes > 0;

            if (_text != null)
            {
                var usedSpaceString = StringFormat.Bytes(StringFormatBytes.Suffix, reservedSpaceBytes);

                if (supportAvailableSpace)
                {
                    var totalSpaceString = StringFormat.Bytes(
                        StringFormatBytes.Suffix,
                        availableFreeSpaceBytes + usedSpaceBytes
                    );

                    _text.text = $"{usedSpaceString} / {totalSpaceString}";
                }
                else
                {
                    _text.text = usedSpaceString;
                }
            }

            if (_fillImage != null)
                _fillImage.fillAmount = reservedSpaceBytes <= 0
                    ? 0
                    : reservedSpaceBytes / (float)(availableFreeSpaceBytes + usedSpaceBytes);

            if (_enableIfAvailableSpaceSupported != null)
                _enableIfAvailableSpaceSupported.SetActive(supportAvailableSpace);

            if (_disableIfAvailableSpaceSupported != null)
                _disableIfAvailableSpaceSupported.SetActive(!supportAvailableSpace);
            
            _isUpdatingUsage = false;
        }
    }
}
