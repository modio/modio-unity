using System;
using System.Threading.Tasks;
using Modio.Authentication;
using Modio.Extensions;
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
        UserProfile _currentUser;

        public void OnUserUpdate(UserProfile user)
        {
            if (user == null || user.UserId <= 0)
            {
                _image.texture = _noUserImage;
                _currentUser = null;

                return;
            }

            // Since this only updates on a user update, we're catching when we have a null user above, so we can
            // neglect doing further checks below.
            _currentUser = user;

            if (_currentUser == User.Current.Profile
                && ModioServices.TryResolve(out IModioAuthService authService)
                && authService is IExternalAvatarProviderService<Texture2D> imageProvider
                && authService.Portal == User.Current.AuthenticatedPortal)
            {
                SetImageFromProvider(imageProvider).ForgetTaskSafely();
                return;
            }

            SetLazyImage();
        }

        void SetLazyImage()
        {
            if (_currentUser is null)
            {
                // Since a request for an image can (technically, tho shouldn't) outlast the time it takes to get a new
                // user, we do this check here too
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

            _lazyImage.SetImage(_currentUser.Avatar, _resolution);
        }

        public async Task SetImageFromProvider(IExternalAvatarProviderService<Texture2D> avatarProvider)
        {
            (Error error, Texture2D image) = await avatarProvider.TryGetAvatarImage();

            if (error)
            {
                ModioLog.Warning?.Log(
                    $"Unable to get avatar from logged in user from {nameof(IExternalAvatarProviderService<Texture2D>)}! Defaulting to mod.io avatar."
                );

                SetLazyImage();
            }
            else if (_image is not null) 
                _image.texture = image;
        }
    }
}
