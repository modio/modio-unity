namespace ModIO.Implementation
{
    /// <summary>Enum representing the result code values.</summary>
    internal static class ResultCode
    {
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

        // - API handling errors -
        public const uint API_FailedToDeserializeResponse = 20300;
        public const uint API_FailedToGetResponseFromWebRequest = 20301;

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
        public const uint IO_InvalidMountPoint = 20430;
        public const uint IO_AccessDenied = 20440;
        public const uint IO_FileSizeTooLarge = 20441;
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

        // 11008   You have been ratelimited for making too many requests. See Rate Limiting.
        public const uint RESTAPI_RateLimitExceeded = 11008;

        // 11012    Invalid security code.
        public const uint RESTAPI_11012 = 11012;

        // 11014    security code has expired. Please request a new code
        public const uint RESTAPI_11014 = 11014;

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

        // 15043   The mod rating is already removed
        public const uint RESTAPI_ModRatingNotFound = 15043;

        // 21000   The requested user could not be found.
        public const uint RESTAPI_UserIdNotFound = 21000;

#endregion
    }
}
