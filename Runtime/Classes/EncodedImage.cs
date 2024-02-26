using UnityEngine;

namespace ModIO
{
    public class EncodedImage
    {
        public string extension;
        public byte[] data;

        public static EncodedImage PNGFromTexture2D(Texture2D texture2D)
        {
            return new EncodedImage
            {
                extension = "png",
                data = texture2D.EncodeToPNG()
            };
        }
    }
}
