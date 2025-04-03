using System;

namespace Modio.Images
{
    public class LazyImage<TImage> where TImage : class
    {
        public event Action<TImage> OnNewImageAvailable;
        public event Action<bool> OnLoadingActive;
        
        ImageReference _currentImageReference;
        BaseImageCache<TImage> _imageCache;

        bool _failedToLoad;

        public LazyImage(BaseImageCache<TImage> imageCache, Action<TImage> onImageAvailable = null, Action<bool> onLoadingActive = null)
        {
            _imageCache = imageCache;
            OnNewImageAvailable = onImageAvailable;
            OnLoadingActive = onLoadingActive;
        }
        
        public async void SetImage<T>(ModioImageSource<T> source, T resolution) where T : Enum
        {
            ImageReference newImageReference = source.GetUri(resolution);
            
            if (newImageReference != _currentImageReference) _failedToLoad = false;
            else if (_failedToLoad) return;
            
            _currentImageReference = newImageReference;
            
            TImage cachedImage = _imageCache.GetCachedImage(_currentImageReference);
            if (cachedImage != null)
            {
                ApplyImage(cachedImage);
                return;
            }

            //get a temp fallback
            TImage fallBackImage = _imageCache.GetFirstCachedImage(source.GetAllReferences());
            if (fallBackImage != null)
            {
                ApplyImage(fallBackImage);
            }
            
            OnLoadingActive?.Invoke(true);

            ImageReference currentlyDownloading = _currentImageReference;
            (Error downloadError, TImage downloadedImage) = await _imageCache.DownloadImage(_currentImageReference);
            
            if (_currentImageReference == currentlyDownloading)
            {
                if (downloadError)
                    _failedToLoad = true;
                
                ApplyImage(downloadedImage);
            }

            OnLoadingActive?.Invoke(false);
        }

        void ApplyImage(TImage cachedImage)
        {
            OnNewImageAvailable?.Invoke(cachedImage);
        }
    }
}
