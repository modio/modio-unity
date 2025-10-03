using System.Runtime.InteropServices;

namespace Modio.FileIO
{
    public class MacDataStorage : BaseDataStorage
    {
        protected override long GetAvailableFreeSpace()
        {
            // Likely the UnixStatsFs object is in a different format to the Linux version, investigate
            /*if (ModioClient.Settings.TryGetPlatformSettings(out ModioDiskTestSettings settings)
                && settings.OverrideDiskSpaceRemaining)
                return settings.BytesRemaining;

            //plugin likely isn't initialized yet
            if (!Initialized) return 0;
            
            var statFs = new UnixStatsFs();
            var result = statvfs("/", out statFs);

            // Linux file systems are split into blocks & block sizes so we check those to accurately gauge.
            // Not all installations will support f_frsize, so we check if its greater than 0 and use block f_bsize
            // if it is 0
            ulong blockSize = statFs.f_frsize > 0 ? statFs.f_frsize : statFs.f_bsize;
            
            ulong availableSpace = statFs.f_bavail * blockSize;
            var availableBytes = (long)availableSpace;
            return availableBytes;*/

            return 0;
        }
        
        // This has to be called with Ansi char set or else we'll only get the stats for the root partition rather than
        // the actual partition our files live on.
        [DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern short statvfs(
            string directory,
            out UnixStatsFs statsFs
        );

        struct UnixStatsFs
        {
            public ulong f_bsize;   // file system block size
            public ulong f_frsize;  // file system fragment size
            public ulong f_blocks;  // size of fs in f_frsize units
            public ulong f_bfree;   // free blocks
            public ulong f_bavail;  // free blocks for non-root (just process?)
            public ulong f_files;   // inodes
            public ulong f_ffre;    // free inodes
            public ulong f_favail;  // free inodes for non-root (just process?)
            public ulong f_fsid;    // file system id
            public ulong f_flag;    // mount flags
            public ulong f_namemax; // maximum filename length
        }
    }
}
