using System;
using Modio.Mods;

namespace Modio.FileIO
{
    internal class ModInstallProgressTracker
    {
        readonly Mod _mod;
        readonly long _totalSize;
            
        DateTime _lastCalculatedAt;
        long _bytesPerSecond;
        long _lastCalculatedSpeedAtBytes;

        public ModInstallProgressTracker(Mod mod, long totalSize)
        {
            _totalSize = totalSize;
            _mod = mod;
        }

        public void SetBytesRead(long currentBytes)
        {
            DateTime currentTime = DateTime.Now;

            // update BytesPerSecond continuously for the first second, then once per second
            float secondsSinceLastCalculated = (float)(currentTime - _lastCalculatedAt).TotalMilliseconds / 1000f;

            if (secondsSinceLastCalculated > 1 || _lastCalculatedSpeedAtBytes == 0)
            {
                _bytesPerSecond = (long)((currentBytes - _lastCalculatedSpeedAtBytes) / secondsSinceLastCalculated);

                if (secondsSinceLastCalculated > 1)
                {
                    _lastCalculatedAt = currentTime;
                    _lastCalculatedSpeedAtBytes = currentBytes;
                }
            }

            // Cap the progress, so it doesn't get to 100% while we wait for the server response
            float progress = 0.99f * currentBytes / _totalSize;
                
            _mod.File.FileStateProgress = progress;
            _mod.File.DownloadingBytesPerSecond = _bytesPerSecond;
            _mod.InvokeModUpdated(ModChangeType.DownloadProgress);
        }
    }
}
