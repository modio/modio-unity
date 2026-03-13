using Modio.Unity.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components
{
    public class ModioUIMonetizationIcon : MonoBehaviour
    {
        [SerializeField]
        Image _image;
        
        void Start()
        {
            if(_image == null)
                _image = GetComponent<Image>();
            
            ModioClient.OnInitialized += GetIcon;
        }

        void OnDestroy()
        {
            ModioClient.OnInitialized -= GetIcon;
        }

        void GetIcon()
        {
            if (ModioClient.Settings.TryGetPlatformSettings(out UnityMonetizationSettings settings))
            {
                _image.sprite = settings.CurrencyIcon;
            }
        }
    }
}
