using System.Runtime.InteropServices;

namespace ModIO.Platform.Mobile.iOS
{
    #if UNITY_IOS

    public static class NativeIosBridge
    {
        [DllImport("__Internal")]
        public static extern long GetAvailableDiskSpace();
    }
#endif

}
