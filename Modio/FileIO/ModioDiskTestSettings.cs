using System;

namespace Modio.FileIO
{
    [Serializable]
    public class ModioDiskTestSettings : IModioServiceSettings
    {
        public bool OverrideDiskSpaceRemaining;
        public int BytesRemaining;
    }
}
