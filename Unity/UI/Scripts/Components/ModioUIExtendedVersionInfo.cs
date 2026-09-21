using System;
using Modio.Settings;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Components
{
    public class ModioUIExtendedVersionInfo : MonoBehaviour
    {
        [SerializeField] TMP_Text _text;

        void Start()
        {
            _text.text = "";
            
            ModioClient.OnInitialized += SetVersionText;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= SetVersionText;
        }

        void SetVersionText()
        {
            string version = Version.GetCurrent();
            
            if (ModioClient.Settings.TryGetPlatformSettings(out ExtendedVersionInfoSettings info))
            {
                version += info.Info;
            }
            
            _text.text = version;
        }
    }
}
