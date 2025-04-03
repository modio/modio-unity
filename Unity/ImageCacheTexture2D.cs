using System;
using System.Threading.Tasks;
using Modio.Images;
using Modio.Users;
using UnityEngine;

namespace Modio.Unity
{
    public class ImageCacheTexture2D : BaseImageCache<Texture2D>
    {
        public static readonly ImageCacheTexture2D Instance = new ImageCacheTexture2D();

        protected override Texture2D Convert(byte[] rawBytes)
        {
            if(rawBytes == null || rawBytes.Length == 0)
            {
                ModioLog.Verbose?.Log(":INTERNAL: Attempted to parse image from NULL/0-length buffer.");
                return null;
            }

            var texture = new Texture2D(0, 0);

            bool success = texture.LoadImage(rawBytes, false);

            if(success) 
                return texture;

            ModioLog.Verbose?.Log(":INTERNAL: Failed to parse image data.");
            return null;
        }

        protected override byte[] ConvertToBytes(Texture2D image) => image != null ? image.EncodeToPNG() : null;
    }

    public static class ModioImageTexture2DExtensions
    {
        public static Task<(Error error, Texture2D texture)> DownloadAsTexture2D(this ImageReference imageReference) 
            => ImageCacheTexture2D.Instance.DownloadImage(imageReference);

        public static Task<(Error error, Texture2D texture)> DownloadAsTexture2D<TResolution>(
            this ModioImageSource<TResolution> imageSource,
            TResolution resolution
        ) where TResolution : Enum
            => ImageCacheTexture2D.Instance.DownloadImage(imageSource.GetUri(resolution));
    }
}
