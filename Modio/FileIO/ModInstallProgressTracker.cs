using System;
using System.Threading;
using Modio.Mods;

namespace Modio.FileIO
{
    public class ModInstallProgressTracker
    {
        readonly Mod _mod;
        readonly long _totalSize;
        readonly SynchronizationContext _synchronizationContext;
        
        Func<long> _currentBytesGetter;

        DateTime _lastCalculatedAt;
        long _bytesPerSecond;
        long _lastCalculatedSpeedAtBytes;
        SendOrPostCallback _sendOrPostCallback;

        public ModInstallProgressTracker(Mod mod, long totalSize, Func<long> currentBytesGetter = null)
        {
            _totalSize = totalSize;
            _currentBytesGetter = currentBytesGetter;
            _mod = mod;

            _synchronizationContext = SynchronizationContext.Current;
        }

        public Func<long> CurrentBytesGetter
        {
            get => _currentBytesGetter;
            set => _currentBytesGetter = value;
        }

        public void Update()
        {
            if (_currentBytesGetter != null)
                SetBytesRead(_currentBytesGetter());
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

            _sendOrPostCallback ??= SetProgressOnMod;
            _synchronizationContext.Post(_sendOrPostCallback, (progress, _bytesPerSecond));
        }

        void SetProgressOnMod(object packedInfo)
        {
            if (_mod.File == null)
                return;
            
            (float progress, long bytesPerSecond) = ((float progress, long bytesPerSecond))packedInfo;
            _mod.File.FileStateProgress = progress;
            _mod.File.DownloadingBytesPerSecond = bytesPerSecond;
            _mod.InvokeModUpdated(ModChangeType.DownloadProgress);
        }
    }
}
