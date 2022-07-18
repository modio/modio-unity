using JetBrains.Annotations;
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
        [CanBeNull]
        public ModId? modId;

        /// <summary>
        /// Whether this mod will appear as public or hidden.
        /// </summary>
        [CanBeNull]
        public bool? visible;

        /// <summary>
        /// Image file which will represent your mods logo. Must be gif, jpg or png format and
        /// cannot exceed 8MB in filesize. Dimensions must be at least 512x288 and we recommend
        /// you supply a high resolution image with a 16 / 9 ratio. mod.io will use this image to
        /// make three thumbnails for the dimensions 320x180, 640x360 and 1280x720
        /// </summary>
        [CanBeNull]
        public Texture2D logo;

        /// <summary>
        /// Image files that will be included in the mod profile details.
        /// </summary>
        [CanBeNull]
        public Texture2D[] images;

        /// <summary>
        /// Name of your mod
        /// </summary>
        [CanBeNull]
        public string name;

        /// <summary>
        /// Path for the mod on mod.io. For example: https://gamename.mod.io/mod-name-id-here.
        /// If no name_id is specified the <see cref="name"/> will be used. For example: 'Stellaris
        /// Shader Mod' will become 'stellaris-shader-mod'. Cannot exceed 80 characters
        /// </summary>
        [CanBeNull]
        public string name_id;

        /// <summary>
        /// Summary for your mod, giving a brief overview of what it's about.
        /// Cannot exceed 250 characters.
        /// </summary>
        /// <remarks>This field must be assigned when submitting a new Mod Profile</remarks>
        [CanBeNull]
        public string summary;

        /// <summary>
        /// Detailed description for your mod, which can include details such as 'About', 'Features',
        /// 'Install Instructions', 'FAQ', etc. HTML supported and encouraged
        /// </summary>
        [CanBeNull]
        public string description;

        /// <summary>
        /// Official homepage for your mod. Must be a valid URL
        /// </summary>
        [CanBeNull]
        public string homepage_url;

        /// <summary>
        /// This will create a cap on the number of subscribers for this mod. Set to 0 to allow
        /// for infinite subscribers.
        /// </summary>
        [CanBeNull]
        public int? maxSubscribers;

        /// <summary>
        /// This is a Bitwise enum so you can assign multiple values
        /// </summary>
        /// <seealso cref="ContentWarnings"/>
        [CanBeNull]
        public ContentWarnings? contentWarning;

        /// <summary>
        /// Your own custom metadata that can be uploaded with the mod profile. (This is for the
        /// entire mod profile, a unique metadata field can be assigned to each modfile as well)
        /// </summary>
        /// <seealso cref="ModfileDetails"/>
        /// <remarks>the metadata has a maximum size of 50,000 characters.</remarks>
        [CanBeNull]
        public string metadata;

        /// <summary>
        /// The tags this mod profile has. Only tags that are supported by the parent game can be
        /// applied. (Invalid tags will be ignored)
        /// </summary>
        [CanBeNull]
        public string[] tags;
    }
}
