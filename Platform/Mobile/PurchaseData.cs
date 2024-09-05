#if (UNITY_IOS || UNITY_ANDROID) && MODIO_MOBILE_IAP
using System;
using UnityEngine.Purchasing;

namespace Plugins.mod.io.Platform.Mobile
{
    [Serializable]
    public class PurchaseData
    {
        public string Payload { get; set; }
        public string PayloadJson { get; set; } = null;
        public Product Product { get; set; }
        public Action CompleteValidation { get; set; }
    }
}
#endif
