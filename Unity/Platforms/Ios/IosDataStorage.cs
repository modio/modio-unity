using System;
using System.Runtime.InteropServices;
using Modio.FileIO;

namespace Modio.Unity.Platforms.Ios
{
    public class IosDataStorage : BaseDataStorage
    {
        protected override long GetAvailableFreeSpace()
        {
#if UNITY_IOS
            try
            {
                return GetAvailableDiskSpace();
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log(e);
                return 0;
            }
#endif
            return 0;
        }

#if UNITY_IOS
        [DllImport("__Internal")]
        static extern long GetAvailableDiskSpace();
#endif
    }
}
