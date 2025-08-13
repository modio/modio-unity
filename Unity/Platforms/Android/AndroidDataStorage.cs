using System;
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
            
            MigrateLegacyModInstalls();

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

        protected override void MigrateLegacyModInstalls()
        {
            var legacyRoot = $"{Path.Combine(Application.persistentDataPath, "mod.io", GameId.ToString())}{Path.DirectorySeparatorChar}";
            
            // We've had to move the Android installs to /UnityCache/ to prevent automatic backups
            // So for instances where mods have been installed on the old path, we need to move them
            MigrateLegacyModInstalls(Path.Combine(legacyRoot, "Installed"));
            MigrateLegacyModInstalls(Path.Combine(legacyRoot, "mods"));

            // The below are smaller with minimal impact, so don't need to be handled as delicately as mod installs
            try
            {
                if (Directory.Exists(Path.Combine(legacyRoot, "ImageCache")))
                    Directory.Delete(Path.Combine(legacyRoot, "ImageCache"), true);

                if (Directory.Exists(Path.Combine(legacyRoot, "Temp")))
                    Directory.Delete(Path.Combine(legacyRoot, "Temp"), true);
            }
            catch (Exception e)
            {
                ModioLog.Warning?.Log($"Exception deleting legacy image cache & temp directories: {e}");
            }
        }
    }
}
