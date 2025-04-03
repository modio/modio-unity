using System;
using System.Collections.Generic;
using Modio.API.SchemaDefinitions;
using Modio.Mods;
using Newtonsoft.Json.Utilities;

namespace Modio.API
{
    /// <summary>
    /// You do not need to use this class. This is used to ensure specific types in API request
    /// objects and anything we serialize gets AOT code generated when using IL2CPP compilation.
    /// </summary>
    public static class AotTypeEnforcer
    {
        public static void Hello()
        {
			AotHelper.EnsureList<AccessTokenObject>();
			AotHelper.EnsureList<AvatarObject>();
			AotHelper.EnsureList<CommentObject>();
			AotHelper.EnsureList<DateTime>();
			AotHelper.EnsureList<DownloadObject>();
			AotHelper.EnsureList<EntitlementDetailsObject>();
			AotHelper.EnsureList<EntitlementFulfillmentObject>();
			AotHelper.EnsureList<Error>();
			AotHelper.EnsureList<ErrorObject>();
			AotHelper.EnsureList<FilehashObject>();
			AotHelper.EnsureList<GameMonetizationTeamObject>();
			AotHelper.EnsureList<GameObject>();
			AotHelper.EnsureList<GameOtherUrlsObject>();
			AotHelper.EnsureList<GamePlatformsObject>();
			AotHelper.EnsureList<GameTagOptionLocalizedObject>();
			AotHelper.EnsureList<GameTagOptionObject.EmbeddedTagsLocalization>();
			AotHelper.EnsureList<GameTagOptionObject>();
			AotHelper.EnsureList<GameTagCategory>();
			AotHelper.EnsureList<ModTag>();
			AotHelper.EnsureList<GuideStatsObject>();
			AotHelper.EnsureList<GuideTagObject>();
			AotHelper.EnsureList<ImageObject>();
			AotHelper.EnsureList<ImageObject>();
			AotHelper.EnsureList<KeyValuePair<string, string>>();
			AotHelper.EnsureList<LineItemsObject>();
			AotHelper.EnsureList<LogoObject>();
			AotHelper.EnsureList<MetadataKvpObject>();
			AotHelper.EnsureList<MetadataKvpObject>();
			AotHelper.EnsureList<ModDependantsObject>();
			AotHelper.EnsureList<ModDependenciesObject>();
			AotHelper.EnsureList<ModDependenciesObject>();
			AotHelper.EnsureList<ModEventObject>();
			AotHelper.EnsureList<ModEventObject>();
			AotHelper.EnsureList<ModId>();
			AotHelper.EnsureList<ModMediaObject>();
			AotHelper.EnsureList<ModObject>();
			AotHelper.EnsureList<ModPlatformsObject>();
			AotHelper.EnsureList<ModStats>();
			AotHelper.EnsureList<ModStatsObject>();
			AotHelper.EnsureList<ModTagObject>();
			AotHelper.EnsureList<ModfileObject>();
			AotHelper.EnsureList<ModfilePlatformObject>();
			AotHelper.EnsureList<MonetizationTeamAccountsObject>();
			AotHelper.EnsureList<MultipartUploadObject>();
			AotHelper.EnsureList<MultipartUploadPartObject>();
			AotHelper.EnsureList<PaymentMethodObject>();
			AotHelper.EnsureList<RatingObject>();
			AotHelper.EnsureList<TeamMemberObject>();
			AotHelper.EnsureList<TermsObject>();
			AotHelper.EnsureList<TransactionObject>();
			AotHelper.EnsureList<UserEventObject>();
			AotHelper.EnsureList<UserObject>();
			AotHelper.EnsureList<WalletObject>();
        }
    }
}
