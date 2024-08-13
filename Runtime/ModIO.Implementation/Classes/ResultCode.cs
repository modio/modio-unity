using ModIO.Implementation.API.Objects;
using System.Collections.Generic;
using System.Linq;

namespace ModIO.Implementation
{
    /// <summary>Enum representing the result code values.</summary>
    internal static class ResultCode
    {
        // When adding a new value make sure it's also added to the errorCodesClearText dictionary.

        #region Value Constants

        // - success -
        public const uint Success = 0;

        // - unknown -
        public const uint Unknown = 1;

        // - init errors -
        public const uint Init_NotYetInitialized = 20000;
        public const uint Init_FailedToLoadConfig = 20010;
        public const uint Init_UserDataFailedToInitialize = 20020;
        public const uint Init_PersistentDataFailedToInitialize = 20021;
        public const uint Init_TemporaryDataFailedToInitialize = 20022;

        public const uint Settings_InvalidServerURL = 20050;
        public const uint Settings_InvalidGameId = 20051;
        public const uint Settings_InvalidGameKey = 20052;
        public const uint Settings_InvalidLanguageCode = 20053;
        public const uint Settings_UploadsDisabled = 20054;
        public const uint Settings_MarketplaceNotEnabled = 20055;
        public const uint Settings_UnableToRetrieveGameProfile = 20056;

        // - auth errors -
        public const uint User_NotAuthenticated = 20100;
        public const uint User_InvalidToken = 20101;
        public const uint User_InvalidEmailAddress = 20102;
        public const uint User_AlreadyAuthenticated = 20103;
        public const uint User_NotRemoved = 20104;

        // - Invalid errors for misuse of the interface -
        public const uint InvalidParameter_PaginationParams = 20201;
        public const uint InvalidParameter_ReportNotReady = 20202;
        public const uint InvalidParameter_ModMetadataTooLarge = 20203;
        public const uint InvalidParameter_BadCreationToken = 20204;
        public const uint InvalidParameter_DescriptionTooLarge = 20205;
        public const uint InvalidParameter_ChangeLogTooLarge = 20206;

        public const uint InvalidParameter_ModProfileRequiredFieldsNotSet = 20210;
        public const uint InvalidParameter_ModSummaryTooLarge = 20211;
        public const uint InvalidParameter_ModLogoTooLarge = 20212;
        public const uint InvalidParameter_CantBeNull = 20213;
        public const uint InvalidParameter_MissingModId = 20214;
        public const uint InvalidParameter_TooMany = 20215;

        public const uint InvalidParameter_DownloadReferenceIsntValid = 20220;

        // - API handling errors -
        public const uint API_FailedToDeserializeResponse = 20300;
        public const uint API_FailedToGetResponseFromWebRequest = 20301;
        public const uint API_FailedToConnect = 20302;
        public const uint API_FailedToCompleteRequest = 20303;

        // - I/O errors -
        public const uint IO_FilePathInvalid = 20400;
        public const uint IO_FileDoesNotExist = 20401;
        public const uint IO_FileCouldNotBeOpened = 20402;
        public const uint IO_FileCouldNotBeCreated = 20403;
        public const uint IO_FileCouldNotBeDeleted = 20404;
        public const uint IO_FileCouldNotBeRead = 20405;
        public const uint IO_FileCouldNotBeWritten = 20406;
        public const uint IO_DirectoryDoesNotExist = 20420;
        public const uint IO_DirectoryCouldNotBeCreated = 20421;
        public const uint IO_DirectoryCouldNotBeDeleted = 20422;
        public const uint IO_DirectoryCouldNotBeMoved = 20423;
        public const uint IO_DirectoryCouldNotBeOpened = 20424;
        public const uint IO_DirectoryCouldNotBeRead = 20425;
        public const uint IO_InvalidMountPoint = 20430;
        public const uint IO_AccessDenied = 20440;
        public const uint IO_FileSizeTooLarge = 20441;
        public const uint IO_InsufficientStorage = 20442;
        public const uint IO_DataServiceForPathNotFound = 20450;

        // - Internal errors for mod.io team -
        public const uint Internal_DuplicateRequestWithDifferingSchemas = 20500;
        public const uint Internal_FailedToDeserializeObject = 20501;
        public const uint Internal_RegistryNotInitialized = 20502;
        public const uint Internal_ModManagementOperationFailed = 20503;
        public const uint Internal_FileSizeMismatch = 20504;
        public const uint Internal_FileHashMismatch = 20505;
        public const uint Internal_OperationCancelled = 20506;
        public const uint Internal_InvalidParameter = 20507;

        // - WSS handling errors -
        public const uint WSS_NotConnected = 20600;
        public const uint WSS_FailedToSend = 20601;
        public const uint WSS_MessageTimeout = 20602;
        public const uint WSS_UnexpectedMessage = 20603;

        // - REST API Errors -
        // 10000   mod.io is currently experiencing an outage. (rare)
        public const uint RESTAPI_ServerOutage = 10000;

        // 10001   Cross-origin request forbidden.
        public const uint RESTAPI_CrossOriginRequestForbidden = 10001;

        // 10002   mod.io failed to complete the request, please try again. (rare)
        public const uint RESTAPI_UnknownServerError = 10002;

        // 10003   API version supplied is invalid.
        public const uint RESTAPI_APIVersionInvalid = 10003;

        // 11000   api_key is missing from your request.
        public const uint RESTAPI_APIKeyMissing = 11000;

        // 11001   api_key supplied is malformed.
        public const uint RESTAPI_APIKeyMalformed = 11001;

        // 11002   api_key supplied is invalid.
        public const uint RESTAPI_APIKeyInvalid = 11002;

        // 11003   Access token is missing the write scope to perform the request.
        public const uint RESTAPI_InsufficientWritePermission = 11003;

        // 11004   Access token is missing the read scope to perform the request.
        public const uint RESTAPI_InsufficientReadPermission = 11004;

        // 11005   Access token is expired, or has been revoked.
        public const uint RESTAPI_OAuthTokenExpired = 11005;

        // 11006   Authenticated user account has been deleted.
        public const uint RESTAPI_UserAccountDeleted = 11006;

        // 11007   Authenticated user account has been banned by mod.io admins.
        public const uint RESTAPI_UserAccountBanned = 11007;

        // 11008   You have been rate limited globally for making too many requests. See Rate Limiting.
        public const uint RESTAPI_RateLimitExceededGlobal = 11008;

        // 11009   You have been rate limited from calling this endpoint again, for making too many requests. See Rate Limiting.
        public const uint RESTAPI_RateLimitExceededEndpoint = 11009;

        // 11011   You have already redeemed this security code.
        public const uint RESTAPI_EmailExchangeCodeAlreadyRedeemed = 11011;

        // 11012   This security code has expired. Please request a new code.
        public const uint RESTAPI_EmailExchangeCodeExpired = 11012;

        // 11013    This security code is for a different API Key. Please contact support.
        public const uint RESTAPI_EmailExchangeDifferentApiKey = 11013;

        // 11014    Invalid security code. Please request a new code.
        public const uint RESTAPI_EmailExchangeInvalidCode = 11014;

        // 11015    Email link already used to auth.
        public const uint RESTAPI_AuthLinkEmailAlreadyUsed = 11015;

        // 11069    error.monetization_iap_connected_portal_account_not_found
        public const uint RESTAPI_PortalAccountNotFound = 11069;

        // 13001   The submitted binary file is corrupted.
        public const uint RESTAPI_SubmittedBinaryCorrupt = 13001;

        // 13002   The submitted binary file is unreadable.
        public const uint RESTAPI_SubmittedBinaryUnreadable = 13002;
        // 13004   You have used the input_json parameter with semantically incorrect JSON.
        public const uint RESTAPI_JSONMalformed = 13004;

        // 13005   The Content-Type header is missing from your request.
        public const uint RESTAPI_ContentHeaderTypeMissing = 13005;

        // 13006   The Content-Type header is not supported for this endpoint.
        public const uint RESTAPI_ContentHeaderTypeNotSupported = 13006;

        // 13007   You have requested a response format that is not supported (JSON only).
        public const uint RESTAPI_ResponseFormatNotSupported = 13007;

        // 13009   The request contains validation errors for the data supplied. See the attached
        // errors field within the Error Object to determine which input failed.
        public const uint RESTAPI_DataValidationErrors = 13009;

        // 14000   The requested resource does not exist.
        public const uint RESTAPI_ResourceIdNotFound = 14000;

        // 14001   The requested game could not be found.
        public const uint RESTAPI_GameIdNotFound = 14001;

        // 14006   The requested game has been deleted.
        public const uint RESTAPI_GameDeleted = 14006;

        // 15004   Already subscribed to a mod (can't subscribe).
        public const uint RESTAPI_ModSubscriptionAlreadyExists = 15004;

        // 15005   Not subscribed to a mod (can't unsubscribe).
        public const uint RESTAPI_ModSubscriptionNotFound = 15005;

        // 15006   You do not have the required permissions to create content for the specified
        // resource
        public const uint RESTAPI_InsufficientCreatePermission = 15006;

        // 15010   The requested modfile could not be found.
        public const uint RESTAPI_ModfileIdNotFound = 15010;

        // 15019   No permission to delete specified resource.
        public const uint RESTAPI_InsufficientDeletePermission = 15019;

        // 15022   The requested mod could not be found.
        public const uint RESTAPI_ModIdNotFound = 15022;

        // 15023   The requested mod has been deleted.
        public const uint RESTAPI_ModDeleted = 15023;

        // 15026   The requested comment could not be found.
        public const uint RESTAPI_CommentIdNotFound = 15026;

        // 15028   The mod rating is already positive/negative
        public const uint RESTAPI_ModRatingAlreadyExists = 15028;

        // 15030   The reported mod is currently unauthorized or deleted and not eligible for reporting.
        public const uint REPORT_RESOURCE_NOT_AVAILABLE_FOR_REPORT	 = 15030;

        // 15043   The mod rating is already removed
        public const uint RESTAPI_ModRatingNotFound = 15043;

        // 21000   The requested user could not be found.
        public const uint RESTAPI_UserIdNotFound = 21000;


        // Codes I need to to do for auth/response cache:
        // from
        // https://docs.mod.io/restapiref/#authenticate-via-steam

        // 11018    The steam encrypted app ticket was invalid.
        public const uint RESTAPI_InvalidSteamEncryptedAppTicket = 11018;

        // 11016    The api_key supplied in the request must be associated with a game.
        public const uint RESTAPI_KeyNotAssociatedWithGame = 11016;

        // 11017    The api_key supplied in the request is for test environment purposes only
        // and cannot be used for this functionality.
        public const uint RESTAPI_TestKeyForTestEnvOnly = 11017;

        // 11019    The secret steam app ticket associated with this game has not been configured.
        public const uint RESTAPI_SecretSteamAppTicketNotConfigured = 11019;

        // 11020    Unable to get steam account data.
        public const uint RESTAPI_SteamUnableToGetAccountData = 11020;

            /* GOG  */

        // 11021   The GOG Galaxy encrypted app ticket was invalid.
        public const uint RESTAPI_GogInvalidAppTicket = 11021;

        // 11022	The secret GOG Galaxy app ticket associated with this game has not been
        // configured.
        public const uint RESTAPI_GogGameNotConfigured = 11022;

        // 11023    Unable to get GOG account data.
        public const uint RESTAPI_GogUnableToGetAccountData = 11023;

            /*  Oculus  */

        // 11024	The secret Oculus Rift app ticket associated with this game has not been
        // configured.
        public const uint RESTAPI_OculusRiftAppTicketNotConfigured = 11024;

        // 11025	The secret Oculus Quest app ticket associated with this game has not been
        // configured.
        public const uint RESTAPI_OculusQuestAppTicketNotConfigured = 11025;

        // 11026    Unable to get Oculus account data.
        public const uint RESTAPI_OculusUnableToGetAccountData = 11026;

            /*  XBOX    */

        // 11027	The Xbox Live token supplied in the request is invalid.
        public const uint RESTAPI_XboxLiveTokenInvalid = 11027;

        // 11028	The user is not permitted to interact with UGC. This can be modified in the
        // user's Xbox Live profile.
        public const uint RESTAPI_XboxNotAllowedToInteractWithUGC = 11028;

        // 11029	The Xbox Live token supplied has expired.
        public const uint RESTAPI_XboxLiveTokenExpired = 11029;

        // 11030	Xbox Live users with 'Child' accounts are not permitted to use mod.io.
        public const uint RESTAPI_XboxLiveChildAccountNotPermitted = 11030;

        // 11042    Unable to get Json Web key signature from Xbox Live.
        public const uint RESTAPI_XboxLiveUnableToGetJwkSignature = 11042;


        // 11031	mod.io was unable to get account data from itch.io servers.
        public const uint RESTAPI_UnableToFetchAccountDataFromItchIo = 11031;

        // 11032    mod.io was unable to verify the credentials against the external service
        // provider.
        public const uint RESTAPI_CantVerifyCredentialsExternally = 11032;

        // 11034    User is already verified by mod.io servers.
        public const uint RESTAPI_UserAlreadyVerified = 11034;

            /*  Nintendo Switch */

        // 11035	The NSA ID token was invalid/malformed.
        public const uint RESTAPI_NsaIdTokenInvalid = 11035;

        // 11039	mod.io was unable to validate the credentials with Nintendo Servers.
        public const uint RESTAPI_UnableToVerifyNintendoCredentials = 11039;

        // 11036	The NSA ID token is not valid yet.
        public const uint RESTAPI_NsaIdTokenNotValidYet = 11036;

        // 11037	The NSA ID token has expired. You should request another token from the Switch
        // SDK and ensure it is delivered to mod.io before it expires.
        public const uint RESTAPI_NsaIdTokenExpired = 11037;

        // 11040	The application ID for the Nintendo Switch title has not been configured,
        // this can be setup in the 'Options' tab within your game profile.
        public const uint RESTAPI_NintendoSwitchAppIdNotConfigured = 11040;

        // 11041	The application ID of the originating Switch title is not permitted to
        // authenticate users. Please check the Switch application id submitted on your games'
        // 'Options' tab and ensure it is the same application id of the Switch title making the
        // authentication request.
        public const uint RESTAPI_NintendoSwitchNotPermittedToAuthUsers = 11041;

            /*   Epic Games */

        // 11044    Invalid Epic Games token.
        public const uint RESTAPI_EpicGamesInvalidToken = 11044;

        // 11045    Attempted to redeem Epic Games token before it's valid.
        // Expired.
        public const uint RESTAPI_EpicGamesInvalidTokenNotValidBefore = 11045;

        // 11046    Attempted to redeem Epic Games token after it's valid.
        // Expired.

        public const uint RESTAPI_EpicGamesINvalidTokenNotValidAFter = 11046;

        // 11048    Unable to get Epic Games json web key Signature
        public const uint RESTAPI_EpicGamesUnableToGetJwkSignature = 11048;

        // 11049    Unable to get Epic Games account data
        public const uint RESTAPI_EpicGamesUnableToGetAccountData = 11049;

            /* PSN  */

        // 11080    Invalid PSN Token
        public const uint RESTAPI_PsnInavalidToken = 11080;

        // 11081    Attempting to redeem PSN token before it's valid.
        // Expired.
        public const uint RESTAPI_PsnInvalidTokenNotValidBefore = 11081;

        // 11082 Attempted to redeem PSN token after it's expired.
        // Expired.
        public const uint RESTAPI_PsnInvalidTokenNotValidAfter = 11082;

        // 11083    Unable to get Json Web Key from PSN servers
        public const uint RESTAPI_PsnUnableToGetJwkSignature = 11083;

        // 11084    Unable to get account data from PSN servers
        public const uint RESTAPI_PsnUnableToGetAccountData = 11084;

        // 11085    PSN child profile is not permitted to access this service
        public const uint RESTAPI_PsnChildProfileNotPermitted = 11085;

        // 11096    The user is not permitted to interact with UGC. This can be modified in the
        // user's PSN profile.
        public const uint RESTAPI_PsnUgcInteractionNotPermitted = 11096;


        // 11051    The user has not agreed to the mod.io Terms of Use.
        // Please see terms_agreed parameter description and the Terms endpoint for more
        // information.
        public const uint RESTAPI_UserMustAgreeToModIoTerms = 11051;

        // 11052	The access token was invalid/malformed.
        public const uint RESTAPI_AccessTokenInvalid = 11052;

        // 11056	mod.io was unable to validate the credentials with Google's servers.
        public const uint RESTAPI_UnableToValidateCredentialsWithGoogle = 11056;

        // 11053	The Google access token is not valid yet.
        public const uint RESTAPI_GoogleAccessTokenNotValidYet = 11053;

        // 11054	The Google access token has expired. You should request another token from the
        // Google SDK and ensure it is delivered to mod.io before it expires.
        public const uint RESTAPI_GoogleAccessTokenExpired = 11054;

        // 11043	mod.io was unable to get account data from the Discord servers.
        public const uint RESTAPI_DiscordUnableToGetAccountData = 11043;

        // 11058    The Facebook access token is invalid.
        public const uint RESTAPI_FacebookInvalidToken = 11058;

        // 11067    Unable to get Facebook account data.
        public const uint RESTAPI_FacebookUnableToGetAccountData = 11067;

        // 11059    User must be logged in before making requests.
        public const uint RESTAPI_UserMustBeLoggedIn = 11059;

        public const uint FILEUPLOAD_Error = 30033;
        #endregion

        private static HashSet<long> cacheClearingErrorCodes =
            new HashSet<long>() {  RESTAPI_OAuthTokenExpired,
                                   RESTAPI_InvalidSteamEncryptedAppTicket,
                                   RESTAPI_CantVerifyCredentialsExternally,
                                   RESTAPI_KeyNotAssociatedWithGame,
                                   RESTAPI_TestKeyForTestEnvOnly,
                                   RESTAPI_SecretSteamAppTicketNotConfigured,
                                   RESTAPI_UserMustAgreeToModIoTerms,
                                   RESTAPI_GogInvalidAppTicket,
                                   RESTAPI_GogGameNotConfigured,
                                   RESTAPI_UnableToFetchAccountDataFromItchIo,
                                   RESTAPI_OculusRiftAppTicketNotConfigured,
                                   RESTAPI_OculusQuestAppTicketNotConfigured,
                                   RESTAPI_XboxLiveTokenInvalid,
                                   RESTAPI_XboxLiveTokenExpired,
                                   RESTAPI_XboxNotAllowedToInteractWithUGC,
                                   RESTAPI_XboxLiveChildAccountNotPermitted,
                                   RESTAPI_NsaIdTokenInvalid,
                                   RESTAPI_UnableToVerifyNintendoCredentials,
                                   RESTAPI_NsaIdTokenNotValidYet,
                                   RESTAPI_NsaIdTokenExpired,
                                   RESTAPI_NintendoSwitchAppIdNotConfigured,
                                   RESTAPI_NintendoSwitchNotPermittedToAuthUsers,
                                   RESTAPI_AccessTokenInvalid,
                                   RESTAPI_UnableToValidateCredentialsWithGoogle,
                                   RESTAPI_GoogleAccessTokenNotValidYet,
                                   RESTAPI_GoogleAccessTokenExpired,
                                   RESTAPI_DiscordUnableToGetAccountData };

        private static Dictionary<uint, string> errorCodesClearText = new Dictionary<uint,
                                                                                     string>() {
            { Success, "Success!" },
            { Unknown, "Unknown" },

            { Init_NotYetInitialized, "Not yet initialized." },
            { Init_FailedToLoadConfig, "Failed to load the config." },
            { Init_UserDataFailedToInitialize, "User data failed to initialize." },
            { Init_PersistentDataFailedToInitialize, "Persistent data failed to initialize." },
            { Init_TemporaryDataFailedToInitialize, "Temporary data failed to initialize." },

            { Settings_InvalidServerURL, "Invalid server URL." },
            { Settings_InvalidGameId, "Invalid game id." },
            { Settings_InvalidGameKey, "Invalid game key." },
            { Settings_InvalidLanguageCode, "Invalid language code." },
            { Settings_UploadsDisabled, "Uploads are disabled." },

            { User_NotAuthenticated, "User not authenticated." },
            { User_InvalidToken, "User has an invalid token." },
            { User_InvalidEmailAddress, "User has an invalid email address." },
            { User_AlreadyAuthenticated, "User already authenticated." },
            { User_NotRemoved, "User not removed." },

            { InvalidParameter_PaginationParams, "Invalid parameter - pagination params." },
            { InvalidParameter_ReportNotReady, "Invalid parameter - report not ready." },
            { InvalidParameter_ModMetadataTooLarge, "Invalid parameter - mod metadata too large." },
            { InvalidParameter_BadCreationToken, "Invalid parameter - bad creation token." },
            { InvalidParameter_DescriptionTooLarge, "Invalid parameter - description too large." },
            { InvalidParameter_ChangeLogTooLarge, "Invalid parameter - changelog too large." },
            { InvalidParameter_ModProfileRequiredFieldsNotSet,
              "Invalid parameter - mod prof.ile required fields not set." },
            { InvalidParameter_ModSummaryTooLarge, "Invalid parameter - mod summary too large." },
            { InvalidParameter_ModLogoTooLarge, "Invalid parameter - mod logo too large." },
            { InvalidParameter_CantBeNull, "Invalid parameter - cant be null!" },
            { InvalidParameter_MissingModId, "Invalid parameter - missing mod id." },

            { API_FailedToDeserializeResponse, "Api failed to deserialize response." },
            { API_FailedToGetResponseFromWebRequest,
              "Api failed to get response from webrequest." },
            { API_FailedToConnect, "Api failed to connect." },
            { API_FailedToCompleteRequest, "Api failed to complete the request." },

            { IO_FilePathInvalid, "IO - File path invalid." },
            { IO_FileDoesNotExist, "IO - File does not exist." },
            { IO_FileCouldNotBeOpened, "IO - File could not be opened." },
            { IO_FileCouldNotBeCreated, "IO - File could not be created." },
            { IO_FileCouldNotBeDeleted, "IO - File could not be deleted." },
            { IO_FileCouldNotBeRead, "IO - File could not be read." },
            { IO_FileCouldNotBeWritten, "IO - File could not be written." },
            { IO_DirectoryDoesNotExist, "IO - Directory does not exist." },
            { IO_DirectoryCouldNotBeCreated, "IO - Directory could not be created." },
            { IO_DirectoryCouldNotBeDeleted, "IO - Directory could not be deleted." },
            { IO_DirectoryCouldNotBeMoved, "IO - Directory could not be moved." },
            { IO_DirectoryCouldNotBeOpened, "IO - Directory could not be opened." },
            { IO_DirectoryCouldNotBeRead, "IO - Directory could not be read." },
            { IO_InvalidMountPoint, "IO - Invalid mount point." },
            { IO_AccessDenied, "IO - Access Denied!" },
            { IO_FileSizeTooLarge, "IO - The filesize is too large." },
            { IO_InsufficientStorage, "IO - There is not enough storage space remaining." },
            { IO_DataServiceForPathNotFound, "IO - data service for path not found." },

            { Internal_DuplicateRequestWithDifferingSchemas,
              "Internal - Duplicate request with differing schemas." },
            { Internal_FailedToDeserializeObject, "Internal - Failed to deserialize object." },
            { Internal_RegistryNotInitialized, "Internal - Registry not initialized." },
            { Internal_ModManagementOperationFailed,
              "Internal - Mod management operation failed." },
            { Internal_FileSizeMismatch, "Internal - File size mismatch." },
            { Internal_FileHashMismatch, "Internal - File hash mismatch." },
            { Internal_OperationCancelled, "Internal - Operation cancelled." },
            { Internal_InvalidParameter, "Internal - Invalid parameter" },

            { RESTAPI_ServerOutage, "mod.io is currently experiencing an outage. (rare)" },
            { RESTAPI_CrossOriginRequestForbidden, "Cross-origin request forbidden." },
            { RESTAPI_UnknownServerError,
              ".io failed to complete the request, please try again. (rare)" },
            { RESTAPI_APIVersionInvalid, "API version supplied is invalid." },
            { RESTAPI_APIKeyMissing, "api_key is missing from your request." },
            { RESTAPI_APIKeyMalformed, "api_key supplied is malformed." },
            { RESTAPI_APIKeyInvalid, "api_key supplied is invalid." },
            { RESTAPI_InsufficientWritePermission,
              "Access token is missing the write scope to perform the request." },
            { RESTAPI_InsufficientReadPermission,
              "Access token is missing the read scope to perform the request." },
            { RESTAPI_OAuthTokenExpired, "Access token is expired, or has been revoked." },
            { RESTAPI_UserAccountDeleted, "Authenticated user account has been deleted." },
            { RESTAPI_UserAccountBanned,
              "Authenticated user account has been banned by mod.io admins." },
            { RESTAPI_RateLimitExceededGlobal,
              "You have been rate limited globally for making too many requests. See Rate Limiting." },
            { RESTAPI_RateLimitExceededEndpoint,
              "You have been rate limited from calling this endpoint again, for making too many requests. See Rate Limiting." },

            //      EMAIL CODES
            { RESTAPI_EmailExchangeCodeAlreadyRedeemed, "Security code already redeemed. Please request a new code."},
            { RESTAPI_EmailExchangeCodeExpired, "Security code has expired. Please request a new code." },
            { RESTAPI_EmailExchangeDifferentApiKey, "Security code is for a different API Key. Please contact support." },
            { RESTAPI_EmailExchangeInvalidCode, "Invalid security code. Please request a new code." },
            { RESTAPI_SubmittedBinaryCorrupt, "The submitted binary file is corrupted." },
            { RESTAPI_SubmittedBinaryUnreadable, "The submitted binary file is unreadable." },
            { RESTAPI_JSONMalformed,
              "You have used the input_json parameter with semantically incorrect JSON." },
            { RESTAPI_ContentHeaderTypeMissing,
              "The Content-Type header is missing from your request." },
            { RESTAPI_ContentHeaderTypeNotSupported,
              "The Content-Type header is not supported for this endpoint." },
            { RESTAPI_ResponseFormatNotSupported,
              "You have requested a response format that is not supported (JSON only)." },
            { RESTAPI_DataValidationErrors,
              "The request contains validation errors for the data supplied. See the attached errors field within the Error Object to determine which input failed." },
            { RESTAPI_ResourceIdNotFound, "The requested resource does not exist." },
            { RESTAPI_GameIdNotFound, "The requested game could not be found." },
            { RESTAPI_GameDeleted, "The requested game has been deleted." },
            { RESTAPI_ModSubscriptionAlreadyExists,
              "Already subscribed to a mod (can't subscribe)." },
            { RESTAPI_ModSubscriptionNotFound, "Not subscribed to a mod (can't unsubscribe)." },
            { RESTAPI_InsufficientCreatePermission,
              "You do not have the required permissions to create content for the specified resource." },
            { RESTAPI_ModfileIdNotFound, "The requested modfile could not be found." },
            { RESTAPI_InsufficientDeletePermission, "No permission to delete specified resource." },
            { RESTAPI_ModIdNotFound, "The requested mod could not be found." },
            { RESTAPI_ModDeleted, "The requested mod has been deleted." },
            { RESTAPI_CommentIdNotFound, "The requested comment could not be found." },
            { RESTAPI_ModRatingAlreadyExists, "The mod rating is already positive/negative" },
            { RESTAPI_ModRatingNotFound, "The mod rating is already removed" },
            { RESTAPI_UserIdNotFound, "The requested user could not be found." },
            { RESTAPI_UserAlreadyVerified, "User is already verified with mod.io servers." },
            { RESTAPI_UserMustBeLoggedIn, "User is not logged in yet. Please log in to complete requests." },


            { RESTAPI_CantVerifyCredentialsExternally,
              "mod.io was unable to verify the credentials against the external service provider." },
            { RESTAPI_KeyNotAssociatedWithGame,
              "The api_key supplied in the request must be associated with a game." },
            { RESTAPI_TestKeyForTestEnvOnly,
              "The api_key supplied in the request is for test environment purposes only and cannot be used for this functionality." },
            { RESTAPI_UserMustAgreeToModIoTerms,
                "The user has not agreed to the mod.io Terms of Use. Please see terms_agreed parameter description and the Terms endpoint for more information." },

            /*  Steam   */
            { RESTAPI_InvalidSteamEncryptedAppTicket,
                "The steam encrypted app ticket was invalid." },
            { RESTAPI_SecretSteamAppTicketNotConfigured,
              "The secret steam app ticket associated with this game has not been configured." },
            { RESTAPI_SteamUnableToGetAccountData,
                "Unable to get account data from Steam, please try again later." },

            /*  GOG */
            { RESTAPI_GogInvalidAppTicket,
                "The GOG Galaxy encrypted app ticket was invalid." },
            { RESTAPI_GogUnableToGetAccountData,
                "Unable to get account data from GOG, please try again later." },
            { RESTAPI_GogGameNotConfigured,
              "The secret GOG Galaxy app ticket associated with this game has not been configured." },

            { RESTAPI_UnableToFetchAccountDataFromItchIo,
              "mod.io was unable to get account data from itch.io servers." },

            /*  Oculus */
            { RESTAPI_OculusRiftAppTicketNotConfigured,
              "The secret Oculus Rift app ticket associated with this game has not been configured." },
            { RESTAPI_OculusQuestAppTicketNotConfigured,
              "The secret Oculus Quest app ticket associated with this game has not been configured." },
            { RESTAPI_OculusUnableToGetAccountData,
                "Unable to get account data from Oculus. Please try again later." },

            /*  XBOX    */
            { RESTAPI_XboxLiveTokenInvalid,
              "The Xbox Live token supplied in the request is invalid." },
            { RESTAPI_XboxLiveTokenExpired,
                "The Xbox Live token supplied has expired." },
            { RESTAPI_XboxNotAllowedToInteractWithUGC,
              "The user is not permitted to interact with UGC. This can be modified in the user's Xbox Live profile." },
            { RESTAPI_XboxLiveChildAccountNotPermitted,
              "Xbox Live users with 'Child' accounts are not permitted to use mod.io." },
            { RESTAPI_XboxLiveUnableToGetJwkSignature,
                "Unable to get the JSON Web Key from Xbox Live. Please try again later." },

            /*  PSN */
            { RESTAPI_PsnInavalidToken,
                "The PSN token supplied in the request is invalid." },
            { RESTAPI_PsnInvalidTokenNotValidAfter,
                "The PSN token has expired. Please request a new token and ensure it is delivered to mod.io before it expires." },
            { RESTAPI_PsnInvalidTokenNotValidBefore,
                "The PSN token is not valid yet." },
            { RESTAPI_PsnUnableToGetAccountData,
                "Unable to get account data from PSN. Please try again later." },
            { RESTAPI_PsnUnableToGetJwkSignature,
                "Unable to get JSON Web Key from PSN. Please try again later." },
            { RESTAPI_PsnChildProfileNotPermitted,
                "Child accounts are not permitted to use external services such as mod.io." },
            { RESTAPI_PsnUgcInteractionNotPermitted,
                "This user is not permitted to interact with UGC. This can be modified in the user's PSN profile." },

            /*  Nintendo Switch  */
            { RESTAPI_NsaIdTokenInvalid,
                "The NSA ID token was invalid/malformed." },
            { RESTAPI_UnableToVerifyNintendoCredentials,
              "mod.io was unable to validate the credentials with Nintendo Servers." },
            { RESTAPI_NsaIdTokenNotValidYet,
                "The NSA ID token is not valid yet." },
            { RESTAPI_NsaIdTokenExpired,
              "The NSA ID token has expired. You should request another token from the Switch SDK and ensure it is delivered to mod.io before it expires." },
            { RESTAPI_NintendoSwitchAppIdNotConfigured,
              "The application ID for the Nintendo Switch title has not been configured, this can be setup in the 'Options' tab within your game profile." },
            { RESTAPI_NintendoSwitchNotPermittedToAuthUsers,
              "The application ID of the originating Switch title is not permitted to authenticate users. Please check the Switch application id submitted on your games' 'Options' tab and ensure it is the same application id of the Switch title making the authentication request." },

            /*  Epic Games  */
            { RESTAPI_EpicGamesInvalidToken,
                "The Epic Games token supplied in the request is invalid." },
            { RESTAPI_EpicGamesINvalidTokenNotValidAFter,
                "The Epic Games token has expired. Please request a new token and ensure it is delivered to mod.io before it expires." },
            { RESTAPI_EpicGamesInvalidTokenNotValidBefore,
                "The Epic Games token is not valid yet." },
            { RESTAPI_EpicGamesUnableToGetAccountData,
                "Unable to get account data from Epic Games. Please try again later." },
            { RESTAPI_EpicGamesUnableToGetJwkSignature,
                "Unable to get JSON Web Key from Epic Games. Please try again later." },

            { RESTAPI_AccessTokenInvalid,
                "//11052 The access token was invalid/malformed." },
            { RESTAPI_UnableToValidateCredentialsWithGoogle,
              "mod.io was unable to validate the credentials with Google's servers." },
            { RESTAPI_GoogleAccessTokenNotValidYet, "The Google access token is not valid yet." },
            { RESTAPI_GoogleAccessTokenExpired,
              "The Google access token has expired. You should request another token from the Google SDK and ensure it is delivered to mod.io before it expires." },
            { RESTAPI_DiscordUnableToGetAccountData,
              "mod.io was unable to get account data from the Discord servers." },
            { RESTAPI_FacebookInvalidToken,
                "The Facebook access token is invalid." },
            { RESTAPI_FacebookUnableToGetAccountData,
                "Unable to get account data from Facebook. Please try again later." },
        };

        public static bool IsInvalidSession(ErrorObject errorObject)
        {
            return cacheClearingErrorCodes.Any(x => x == errorObject.error.error_ref);
        }

        public static string GetErrorCodeMeaning(uint code)
        {
            if (errorCodesClearText.TryGetValue(code, out var meaning))
            {
                return meaning;
            }
            return "This code is not contained in the error codes clear text repository.";
        }
    }
}
