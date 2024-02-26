using System.Collections.Generic;
using UnityEngine;

namespace ModIO
{
    /// <summary>
    /// Use this class to fill out the details of a Mod Profile that you'd like to create or edit.
    /// If you're submitting this via CreateModProfile you must assign values to logo, name and
    /// summary, otherwise the submission will be rejected (All fields except modId are optional if
    /// submitting this via EditModProfile)
    /// </summary>
    /// <seealso cref="ModIOUnity.CreateModProfile"/>
    /// <seealso cref="ModIOUnity.EditModProfile"/>
    public class ModProfileDetails
    {
        /// <summary>
        /// Make sure to set this field when submitting a request to Edit a Mod Profile
        /// </summary>
        /// <remarks>Can be null</remarks>
        public ModId? modId;

        /// <summary>
        /// Whether this mod will appear as public or hidden.
        /// </summary>
        /// <remarks>Can be null</remarks>
        public bool? visible;

        /// <summary>
        /// Image file which will represent your mods logo. Must be gif, jpg or png format and
        /// cannot exceed 8MB in filesize. Dimensions must be at least 512x288 and we recommend
        /// you supply a high resolution image with a 16 / 9 ratio. mod.io will use this image to
        /// make three thumbnails for the dimensions 320x180, 640x360 and 1280x720
        /// </summary>
        /// <remarks>Can be null if using EditModProfile</remarks>
        /// <seealso cref="ModIOUnity.EditModProfile"/>
#if UNITY_2019_4_OR_NEWER
        public Texture2D logo;
        /// <summary>
        /// Logo encoded in supported format.
        /// </summary>
        /// <seealso cref="logo"/>
        public EncodedImage encodedLogo;
#else
        public byte[] logo;
#endif

        /// <summary>
        /// Image files that will be included in the mod profile details.
        /// </summary>
        /// <remarks>Can be null</remarks>
#if UNITY_2019_4_OR_NEWER
        public Texture2D[] images;
        /// <summary>
        /// Images encoded in supported format.
        /// </summary>
        /// <seealso cref="images"/>
        public IReadOnlyList<EncodedImage> encodedImages;
#else
        public List<byte[]> images;
#endif

        /// <summary>
        /// Name of your mod
        /// </summary>
        /// <remarks>Can be null if using EditModProfile</remarks>
        /// <seealso cref="ModIOUnity.EditModProfile"/>
        public string name;

        /// <summary>
        /// Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here.
        /// If no name_id is specified the <see cref="name"/> will be used. For example: 'Stellaris
        /// Shader Mod' will become 'stellaris-shader-mod'. Cannot exceed 80 characters
        /// </summary>
        /// <remarks>Can be null</remarks>
        public string name_id;

        /// <summary>
        /// Summary for your mod, giving a brief overview of what it's about.
        /// Cannot exceed 250 characters.
        /// </summary>
        /// <remarks>This field must be assigned when submitting a new Mod Profile</remarks>
        /// <remarks>Can be null if using EditModProfile</remarks>
        /// <seealso cref="ModIOUnity.EditModProfile"/>
        public string summary;

        /// <summary>
        /// Detailed description for your mod, which can include details such as 'About', 'Features',
        /// 'Install Instructions', 'FAQ', etc. HTML supported and encouraged
        /// </summary>
        /// <remarks>Can be null</remarks>
        public string description;

        /// <summary>
        /// Official homepage for your mod. Must be a valid URL
        /// </summary>
        /// <remarks>Can be null</remarks>
        public string homepage_url;

        /// <summary>
        /// This will create a cap on the number of subscribers for this mod. Set to 0 to allow
        /// for infinite subscribers.
        /// </summary>
        /// <remarks>Can be null</remarks>
        public int? maxSubscribers;

        /// <summary>
        /// This is a Bitwise enum so you can assign multiple values
        /// </summary>
        /// <seealso cref="ContentWarnings"/>
        /// <remarks>Can be null</remarks>
        public ContentWarnings? contentWarning;

        /// <summary>
        /// Your own custom metadata that can be uploaded with the mod profile. (This is for the
        /// entire mod profile, a unique metadata field can be assigned to each modfile as well)
        /// </summary>
        /// <seealso cref="ModfileDetails"/>
        /// <remarks>the metadata has a maximum size of 50,000 characters.</remarks>
        /// <remarks>Can be null</remarks>
        public string metadata;

        /// <summary>
        /// The tags this mod profile has. Only tags that are supported by the parent game can be
        /// applied. (Invalid tags will be ignored)
        /// </summary>
        /// <remarks>Can be null</remarks>
        public string[] tags;

        /// <summary>
        ///	Select which interactions players can have with your mod.
        /// 0 = None
        /// 1 = Ability to comment (default)
        /// ? = Add the options you want together, to enable multiple options
        /// </summary>
        /// <remarks>Can be null</remarks>
        public CommunityOptions? communityOptions = CommunityOptions.AllowCommenting;

        internal bool HasLogo()
        {
            return logo != null || encodedLogo != null;
        }

        internal EncodedImage GetLogo()
        {
#if UNITY_2019_4_OR_NEWER
            // If a Texture2D type is not set to 'Sprite (2D or UI)' it will get flagged
            // by cloudflare as suspicious and be rejected. This will return a 403
            if (encodedLogo == null)
                encodedLogo = EncodedImage.PNGFromTexture2D(logo);
            return encodedLogo;
#else
            return logo;
#endif
        }

        internal bool HasGalleryImages()
        {
            return encodedImages != null || images != null;
        }

        internal List<byte[]> GetGalleryImages()
        {
#if UNITY_2019_4_OR_NEWER
            if (encodedImages == null)
            {
                List<EncodedImage> gallery = new List<EncodedImage>();
                foreach(var texture in images)
                {
                    gallery.Add(EncodedImage.PNGFromTexture2D(texture));
                }
                encodedImages = gallery;
            }
            return encodedImages;
#else
            return images;
#endif
        }
    }
}
