using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Modio.FileIO;
using UnityEngine;

namespace Modio.Unity.Platforms.Android
{
    public class AndroidDataStorage : BaseDataStorage
    {
        static AndroidDataStorage() => AndroidJNI.AttachCurrentThread();

        ~AndroidDataStorage() => AndroidJNI.DetachCurrentThread();

        public override Task<Error> Init()
        {
            GameId = ModioServices.Resolve<ModioSettings>().GameId;
            
            // Android paths need / forward slash
            Root = Path.Join(ModioServices.Resolve<IModioRootPathProvider>().Path,
                             $"mod.io{Path.DirectorySeparatorChar}{GameId}{Path.DirectorySeparatorChar}");

            if (!DoesDirectoryExist(Root))
                CreateDirectory(Root);
            
            UserRoot = Path.Join(ModioServices.Resolve<IModioRootPathProvider>().UserPath, 
                                 $"Modio{Path.DirectorySeparatorChar}{GameId}{Path.DirectorySeparatorChar}");

            OngoingTaskCount = 0;
            ShutdownTokenSource = new CancellationTokenSource();
            ShutdownToken = ShutdownTokenSource.Token;

            IsShuttingDown = false;
            Initialized = true;

            return Task.FromResult(Error.None);
        }

        protected override long GetAvailableFreeSpace()
        {
            if (ModioClient.Settings.TryGetPlatformSettings(out ModioDiskTestSettings settings)
                && settings.OverrideDiskSpaceRemaining)
                return settings.BytesRemaining;

            //plugin likely isn't initialized yet
            if (!Initialized) return 0;
            
            var statFs = new AndroidJavaObject("android.os.StatFs", Root);
            var availableBytes = statFs.Call<long>("getFreeBytes");
            return availableBytes;
        }
    }
}
