#if UNITY_IOS || UNITY_ANDROID
#if MODIO_IN_APP_PURCHASING

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
#endif