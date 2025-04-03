using System;
using Modio.Images;
using Modio.Users;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.UserProperties
{
    [Serializable, MovedFrom(true, sourceClassName: "ModPropertyCreatorAvatar")]
    public class UserPropertyCreatorAvatar : IUserProperty
    {
        [SerializeField] RawImage _image;
        [SerializeField] UserProfile.AvatarResolution _resolution = UserProfile.AvatarResolution.X50_Y50;
        [SerializeField] bool _useHighestAvailableResolutionAsFallback = true;
        [SerializeField] Texture _noUserImage;
        LazyImage<Texture2D> _lazyImage;

        public void OnUserUpdate(UserProfile user)
        {
            if (user == null)
            {
                _image.texture = _noUserImage;

                return;
            }

            _lazyImage ??= new LazyImage<Texture2D>(
                ImageCacheTexture2D.Instance,
                texture2D =>
                {
                    if (_image != null) _image.texture = texture2D;
                }
            );
            
            _lazyImage.SetImage(user.Avatar, _resolution);
        }
    }
}
