using System;
using System.Collections.Generic;

namespace Modio.Images
{ 
    public class ModioImageSource<TResolution> where TResolution:Enum
    {
        public string FileName { get; private set; }
        
        readonly ImageReference[] _resolutions;
        bool _isCachingLowestResolution;

        internal ModioImageSource(string fileName, params string[] links)
        {
            FileName = fileName;
            _resolutions = new ImageReference[links.Length];
            
            
            for (var i = 0; i < _resolutions.Length; i++) 
                _resolutions[i] = new ImageReference(links[i]);
        }

        public ImageReference GetUri(TResolution resolution)
        {
            var index = (int)(object)resolution;
            index = Math.Min(_resolutions.Length - 1, index);
            return _resolutions[index];
        }

        public IEnumerable<ImageReference> GetAllReferences() => _resolutions;

        public void CacheLowestResolutionOnDisk(bool shouldCache)
        {
            if (_isCachingLowestResolution == shouldCache) return;
            _isCachingLowestResolution = shouldCache;
            if (_resolutions.Length > 0) BaseImageCache.CacheToDisk(_resolutions[0], shouldCache);
        }
    }
}
