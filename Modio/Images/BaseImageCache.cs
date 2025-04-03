using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Modio.Errors;

namespace Modio.Images
{
    public abstract class BaseImageCache
    {
        protected static readonly List<BaseImageCache> ImageCacheInstances = new List<BaseImageCache>();
        protected static readonly HashSet<ImageReference> PendingDiskSaves = new HashSet<ImageReference>();

        public static void CacheToDisk(ImageReference image, bool shouldCache)
        {
            if(!shouldCache)
            {
                ModioClient.DataStorage.DeleteCachedImage(new Uri(image.Url));
                PendingDiskSaves.Remove(image);
                return;
            }

            var anySaveSuccessful = false;

            foreach (BaseImageCache baseImageCache in ImageCacheInstances)
                anySaveSuccessful |= baseImageCache.CacheToDiskInternal(image);

            if (!anySaveSuccessful) PendingDiskSaves.Add(image);
        }

        protected abstract bool CacheToDiskInternal(ImageReference imageReference);
    }
    
    public abstract class BaseImageCache<T> : BaseImageCache where T : class
    {
        readonly Dictionary<ImageReference, (Error, T)> _cache = new Dictionary<ImageReference, (Error, T)>();
        
        readonly Dictionary<ImageReference, Task<(Error, T)>> _ongoingDownloads
            = new Dictionary<ImageReference, Task<(Error, T)>>();
        
        protected BaseImageCache()
        {
            ImageCacheInstances.Add(this);
        }

        public T GetCachedImage(ImageReference uri)
        {
            _cache.TryGetValue(uri, out (Error, T) cached);
            return cached.Item2;
        }
        
        public Task<(Error errror, T image)> DownloadImage(ImageReference uri)
        {
            if (_cache.TryGetValue(uri, out (Error, T) cached))
                return Task.FromResult(cached);
                
            if (_ongoingDownloads.TryGetValue(uri, out Task<(Error, T)> ongoing))
            {
                return ongoing;
            }

            Task<(Error, T)> downloadTask = DownloadImageInternal(uri);
            
            if(!downloadTask.IsCompleted)
                _ongoingDownloads[uri] = downloadTask;

            return downloadTask;
        }

        async Task<(Error, T)> DownloadImageInternal(ImageReference uri)
        {
            (Error error, Stream stream) = await ModioClient.Api.DownloadFile(uri.Url);
            
            if (error || stream == null)
            {
                var loadFromCached = await LoadFromDiskCache(uri);
                
                if(error.Code != ErrorCode.SHUTTING_DOWN)
                    ModioLog.Warning?.Log($"Error downloading file at {uri.Url}: {error}");
                
                _ongoingDownloads.Remove(uri);
                return (Error.None, loadFromCached);
            }

            var memoryStream = new MemoryStream(1024*1024);
            await stream.CopyToAsync(memoryStream);

            byte[] rawBytes = memoryStream.ToArray();
            T downloadedImage = Convert(rawBytes);

            _cache[uri] = (Error.None, downloadedImage);
            _ongoingDownloads.Remove(uri);

            if (PendingDiskSaves.Contains(uri))
            {
                ModioClient.DataStorage.WriteCachedImage(new Uri(uri.Url), rawBytes);
                PendingDiskSaves.Remove(uri);
            }
            
            return (Error.None, downloadedImage);
        }

        async Task<T> LoadFromDiskCache(ImageReference imageReference)
        {
            var uri = new Uri(imageReference.Url);

            (Error error, byte[] result) = await ModioClient.DataStorage.ReadCachedImage(uri);

            if (error) return null;

            T image = Convert(result);

            PendingDiskSaves.Remove(imageReference);
            _cache[imageReference] = (Error.None, image);
            return image;
        }

        public T GetFirstCachedImage(IEnumerable<ImageReference> imageReferences)
        {
            foreach (ImageReference imageReference in imageReferences)
            {
               T cachedImage = GetCachedImage(imageReference);
                
                if (cachedImage != null)
                {
                    return cachedImage;
                }
            }

            return null;
        }

        protected override bool CacheToDiskInternal(ImageReference imageReference)
        {
            var uri = new Uri(imageReference.Url);

            T cachedImage = GetCachedImage(imageReference);

            if (cachedImage == null) return false;
            
            byte[] bytes = ConvertToBytes(cachedImage);
            ModioClient.DataStorage.WriteCachedImage(uri, bytes);
            return true;

        }

        protected abstract T Convert(byte[] rawBytes);
        protected abstract byte[] ConvertToBytes(T image);
    }
    
    public class ImageCacheBytes : BaseImageCache<byte[]>
    {
        public static readonly ImageCacheBytes Instance = new ImageCacheBytes();
        
        protected override byte[] Convert(byte[] rawBytes) => rawBytes;

        protected override byte[] ConvertToBytes(byte[] image) => image;
    }
}
