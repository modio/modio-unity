using System;

#if MODIO_MOBILE_IAP
using UnityEngine.Purchasing;
#endif

namespace Modio.Unity.Platforms.MobilePurchasing
{
#if MODIO_MOBILE_IAP
    [Serializable]
    public class PurchaseData
    {
        public string PayloadRaw { get; set; }
        public string PayloadJson { get; set; } = null;
        public Product Product { get; set; }
    }
#endif
}
