using System.Threading.Tasks;
using ModIO;
using ModIO.Util;
using UnityEngine;
using UnityEngine.UI;

namespace ModIOBrowser.Implementation
{   
    
    public class Avatar : SelfInstancingMonoSingleton<Avatar>
    {       
        [SerializeField] public Image Avatar_Main;
        [SerializeField] public Image AvatarDownloadBar;

        [Header("Platform Avatar Icons")]
        [SerializeField] public Image PlatformIcon_Main;
        [SerializeField] public Image PlatformIcon_DownloadQueue;
        [SerializeField] public Sprite switchAvatar;
        [SerializeField] public Sprite SteamAvatar;
        [SerializeField] public Sprite XboxAvatar;
        [SerializeField] public Sprite PlayStationAvatar;
        
        private async Task<Sprite> GetSprite(UserPortal currentAuthenticationPortal, UserProfile currentUserProfile)
        {
            switch(currentAuthenticationPortal)
            {
                case UserPortal.Nintendo:
                    return switchAvatar;
                    
                case UserPortal.Steam:
                    return SteamAvatar;
                    
                case UserPortal.XboxLive:
                    return XboxAvatar;

                case UserPortal.PlayStationNetwork:
                    return PlayStationAvatar;
            }

            currentUserProfile = await GetCurrentUser(currentUserProfile);
            var sprite = await DownloadSprite(currentUserProfile.avatar_50x50); 
            return sprite;
        }
        
        public void SetupUser() => 
            SetupUser(Authentication.Instance.currentAuthenticationPortal,
                Authentication.Instance.currentUserProfile);

        private async void SetupUser(UserPortal currentAuthenticationPortal, UserProfile currentUserProfile)
        {
            var sprite = await GetSprite(currentAuthenticationPortal, currentUserProfile);

            if (sprite == null || !Authentication.Instance.IsAuthenticated)
            {
                ShowDefaultAvatar();
                return;
            }
            
            if(currentAuthenticationPortal == UserPortal.None)
            {
                PlatformFree(sprite);
            }
            else
            {
                Platform(sprite);
            }
        }

        void ShowDefaultAvatar()
        {
            Avatar_Main.gameObject.SetActive(false);
            DownloadQueue.Instance.Avatar_DownloadQueue.gameObject.SetActive(false);
            PlatformIcon_Main.transform.parent.gameObject.SetActive(false);
            PlatformIcon_DownloadQueue.transform.parent.gameObject.SetActive(false);
        }

        private void PlatformFree(Sprite sprite)
        {
            // turn on main avatar image
            Avatar_Main.gameObject.SetActive(true);
            DownloadQueue.Instance.Avatar_DownloadQueue.gameObject.SetActive(true);

            // turn off platform icon
            PlatformIcon_Main.transform.parent.gameObject.SetActive(false);
            PlatformIcon_DownloadQueue.transform.parent.gameObject.SetActive(false);

            // change sprites
            Avatar_Main.sprite = sprite;
            DownloadQueue.Instance.Avatar_DownloadQueue.sprite = sprite;
        }

        private void Platform(Sprite sprite)
        {
            // turn off main avatar icons
            Avatar_Main.gameObject.SetActive(false);
            DownloadQueue.Instance.Avatar_DownloadQueue.gameObject.SetActive(false);

            // turn on platform icon
            PlatformIcon_Main.transform.parent.gameObject.SetActive(true);
            PlatformIcon_DownloadQueue.transform.parent.gameObject.SetActive(true);

            // change sprites
            PlatformIcon_Main.sprite = sprite;
            PlatformIcon_DownloadQueue.sprite = sprite;
        }

        internal async Task<UserProfile> GetCurrentUser(UserProfile currentUserProfile)
        {
            ResultAnd<UserProfile> resultAnd = await ModIOUnityAsync.GetCurrentUser();
            
            return resultAnd.result.Succeeded() ? resultAnd.value : currentUserProfile;
        }

        private async Task<Sprite> DownloadSprite(DownloadReference reference)
        {
            ResultAnd<Texture2D> resultTexture = await ModIOUnityAsync.DownloadTexture(reference);

            if(resultTexture.result.Succeeded())
            {
                Sprite sprite = Sprite.Create(resultTexture.value,
                    new Rect(0, 0, resultTexture.value.width, resultTexture.value.height), Vector2.zero);

                return sprite;
            }

            return null;
        }

        internal void UpdateDownloadProgressBar(ProgressHandle handle)
            => AvatarDownloadBar.fillAmount = handle == null ? 0f : handle.Progress;        
    }
}
