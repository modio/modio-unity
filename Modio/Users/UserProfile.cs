using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Caching;
using Modio.Errors;
using Modio.Images;
using Modio.Reports;

namespace Modio.Users
{
    /// <summary>
    /// Represents a particular mod.io user with their username, DownloadReferences for getting
    /// their avatar, as well as their language and timezone.
    /// </summary>
    [System.Serializable]
    public class UserProfile : IEquatable<UserProfile>
    {
        // Since users are not unique per game, we don't need to invalidate this between shutdowns
        static Dictionary<long, UserProfile> _cache = new Dictionary<long, UserProfile>();
        
        public event Action OnProfileUpdated;

        public override int GetHashCode() => UserId.GetHashCode();

        /// <summary>
        /// The display name of the user's mod.io account
        /// </summary>
        public string Username { get; internal set; }

        /// <summary>
        ///  This is the unique Id of the user.
        /// </summary>
        public long UserId { get; internal set; }

        /// <summary>
        /// The display name of the user's account they authenticated with. Eg if they authenticated
        /// with Steam it would be their Steam username.
        /// </summary>
        public string PortalUsername { get; private set; }

        public Wallet GetWallet() => UserId == User.Current.Profile.UserId ? //local user
            User.Current.Wallet : null;

        public enum AvatarResolution
        {
            X50_Y50,
            X100_Y100,
            Original,
        }

        public ModioImageSource<AvatarResolution> Avatar { get; private set; }

        public string Timezone { get; private set; }

        public string Language { get; private set; }

        internal UserProfile(UserObject userObject) => ApplyDetailsFromUserObject(userObject);

        internal UserProfile(){}

        internal void ApplyDetailsFromUserObject(UserObject userObject)
        {
            Username = userObject.Username;
            UserId = userObject.Id;
            PortalUsername = userObject.DisplayNamePortal;
            Timezone = userObject.Timezone;
            Language = userObject.Language;

            Avatar = new ModioImageSource<AvatarResolution>(
                userObject.Avatar.Filename,
                userObject.Avatar.Thumb50X50,
                userObject.Avatar.Thumb100X100,
                userObject.Avatar.Original
            );

            _cache[UserId] = this;
            
            OnProfileUpdated?.Invoke();
        }
        
        internal static UserProfile Get(UserObject user)
        {
            if (!_cache.TryGetValue(user.Id, out UserProfile profile)) 
                return new UserProfile(user);
            
            profile.ApplyDetailsFromUserObject(user);
            return profile;
        }

        public static bool operator ==(UserProfile left, UserProfile right) => Equals(left, right);

        public static bool operator !=(UserProfile left, UserProfile right) => !Equals(left, right);

        public bool Equals(UserProfile other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return UserId == other.UserId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UserProfile)obj);
        }

#region Muting

        public async Task<Error> Mute()
        {
            (Error error, Response204? _) = await ModioAPI.Users.MuteAUser(UserId);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error muting user {Username}: {error}");
                return error;
            }
            
            ModCache.ClearModSearchCache();

            return Error.None;
        }
        
        public async Task<Error> UnMute()
        {
            (Error error, Response204? _) = await ModioAPI.Users.UnmuteAUser(UserId);

            if (error)
            {
                if (!error.IsSilent) ModioLog.Error?.Log($"Error un-muting user {Username}: {error}");
                return error;
            }
            
            ModCache.ClearModSearchCache();

            return Error.None;
        }

#endregion
        
#region Reporting
        public async Task<Error> Report(ReportType reportType, string contact, string summary)
        {
            if (User.Current == null || !User.Current.IsAuthenticated) return (Error) ErrorCode.USER_NOT_AUTHENTICATED;
            
            var request = new AddReportRequest(
                ReportResourceTypes.USERS,
                UserId,
                (long)reportType,
                0,
                null,
                User.Current.Profile.Username,
                contact,
                summary
            );

            var (error, response) = await ModioAPI.Reports.SubmitReport(request);
            return (Error)error;
        }

#endregion
    }
}
