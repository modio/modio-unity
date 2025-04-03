using System;
using System.Collections.Generic;

namespace Modio.Images
{
    /// <summary>
    /// DownloadReference that contains the URL to download an image with.
    /// (DownloadReference is serializable with Unity's JsonUtility)
    /// </summary>
    [System.Serializable]
    public struct ImageReference : IEquatable<ImageReference>
    {
        /// <summary>
        /// Check if there is a valid url for this image. You may want to check this before using
        /// </summary>
        /// <returns>true if the url isn't null</returns>
        public bool IsValid => !string.IsNullOrWhiteSpace(Url);
        
        public string Url { get; private set; }

        internal ImageReference(string url) => Url = url;

        sealed class UrlEqualityComparer : IEqualityComparer<ImageReference>
        {
            public bool Equals(ImageReference x, ImageReference y)
            {
                return x.Url == y.Url;
            }

            public int GetHashCode(ImageReference obj)
            {
                return (obj.Url != null ? obj.Url.GetHashCode() : 0);
            }
        }

        public static bool operator ==(ImageReference left, ImageReference right) => left.Equals(right);

        public static bool operator !=(ImageReference left, ImageReference right) => !left.Equals(right);

        public bool Equals(ImageReference other) => Url == other.Url;

        public override bool Equals(object obj) => obj is ImageReference other && Equals(other);

        public override int GetHashCode() => (Url != null ? Url.GetHashCode() : 0);
    }
}
