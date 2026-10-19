using System.Collections.Generic;
using System.Linq;
using Modio.Mods.Builder;
using UnityEngine;

namespace Modio.Unity.Extensions
{
    public static class ModBuilderExtensions
    {
        /// <remarks>
        /// This will encode the image to a PNG regardless of its original format
        /// </remarks>
        public static ModBuilder SetLogo(this ModBuilder builder, Texture2D image)
        {
            byte[] bytes = image.ConvertToByteArray();

            return builder.SetLogo(bytes, ImageFormat.Png);
        }
        
        /// <remarks>
        /// This will encode the image to a PNG regardless of its original format
        /// </remarks>
        public static ModBuilder SetGallery(this ModBuilder builder, Texture2D image)
        {
            byte[] bytes = image.ConvertToByteArray();

            return builder.SetGallery(bytes, ImageFormat.Png);
        }
        
        /// <remarks>
        /// This will encode the images to a PNG regardless of its original format
        /// </remarks>
        public static ModBuilder SetGallery(this ModBuilder builder, ICollection<Texture2D> images)
        {
            byte[][] bytes = images.Select(texture => texture.ConvertToByteArray()).ToArray();

            return builder.SetGallery(bytes, ImageFormat.Png);
        }

        /// <remarks>
        /// This will encode the image to a PNG regardless of its original format
        /// </remarks>
        public static ModBuilder AppendGallery(this ModBuilder builder, Texture2D image)
        {
            byte[] bytes = image.ConvertToByteArray();

            return builder.AppendGallery(bytes, ImageFormat.Png);
        }
        
        /// <remarks>
        /// This will encode the images to a PNG regardless of its original format
        /// </remarks>
        public static ModBuilder AppendGallery(this ModBuilder builder, ICollection<Texture2D> images)
        {
            byte[][] bytes = images.Select(texture => texture.ConvertToByteArray()).ToArray();

            return builder.AppendGallery(bytes, ImageFormat.Png);
        }

        /// <summary>
        /// Unity imports textures as not readable by default. This is due to it needing to store memory when marking
        /// the texture as readable and is tedious for creators to do for every image. Having all images marked as
        /// readable So instead is also incredibly memory intensive, so instead we handle it by creating a new texture
        /// here in code which defaults isReadable to true
        /// </summary>
        static byte[] ConvertToByteArray(this Texture2D texture)
        {
            var image = new Texture2D(texture.width, texture.height, texture.format, texture.mipmapCount > 1f);
            
            Graphics.CopyTexture(texture, image);

            return image.EncodeToPNG();
        }
    }
}
