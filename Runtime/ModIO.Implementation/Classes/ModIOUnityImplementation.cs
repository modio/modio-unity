using ModIO.Implementation.API;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.API.Requests;
using ModIO.Implementation.Platform;
using ModIO.Implementation.Wss;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using GameObject = ModIO.Implementation.API.Objects.GameObject;

namespace ModIO.Implementation
{

    /// <summary>
    /// The actual implementation for methods called from the ModIOUnity interface
    /// </summary>
    internal static partial class ModIOUnityImplementation
    {
        /// <summary>
        /// A cached reference to the current upload operation handle.
        /// </summary>
        static ProgressHandle currentUploadHandle;

        /// <summary>
        /// Everytime an implemented method with a callback is used it creates a
        /// TaskCompletionSource and adds it to this hashset. Shutdown will make sure to wait for
        /// all of these callbacks to return before invoking the final shutdown callback.
        /// </summary>
        static Dictionary<TaskCompletionSource<bool>, Task> openCallbacks_dictionary =
            new Dictionary<TaskCompletionSource<bool>, Task>();

        static Dictionary<string, Task<ResultAnd<byte[]>>> onGoingImageDownloads = new Dictionary<string, Task<ResultAnd<byte[]>>>();

        /// <summary>
        /// cached Task of the shutdown operation so we dont run several shutdowns simultaneously
        /// </summary>
        static Task shutdownOperation;

        internal static OpenCallbacks openCallbacks = new OpenCallbacks();

        #region Synchronous Requirement Checks - to detect early outs and failures

        /// <summary>Has the plugin been initialized.</summary>
        internal static bool isInitialized;

        internal static GameObject? GameProfile;

        /// <summary>
        /// Flagged to true if the plugin is being shutdown
        /// </summary>
        public static bool shuttingDown;

        //Whether we auto initialize after the first call to the plugin
        static bool autoInitializePlugin = false;

        //has the autoInitializePlugin been set using SettingsAsset
        static bool autoInitializePluginSet = false;

        public static bool AutoInitializePlugin
        {
            get
            {
                if (!autoInitializePluginSet)
                {
                    var result = SettingsAsset.TryLoad(out autoInitializePlugin);
                    if (!result.Succeeded())
                        Logger.Log(LogLevel.Error, result.message);
                    autoInitializePluginSet = true;
                }

                return autoInitializePlugin;
            }
            //Ignore the value in config
            set
            {
                autoInitializePluginSet = true;
                autoInitializePlugin = value;
            }
        }

        /// <summary>Has the plugin been initialized.</summary>
        public static bool IsInitialized(out Result result)
        {
            if (isInitialized)
            {
                result = ResultBuilder.Success;
                return true;
            }

            if (AutoInitializePlugin)
            {
                Debug.Log("Auto initialized");
                result = InitializeForUser("Default");
                if (result.Succeeded())
                {
                    result = ResultBuilder.Success;
                    return true;
                }
            }

            result = ResultBuilder.Create(ResultCode.Init_NotYetInitialized);
            Logger.Log(
                LogLevel.Error,
                "You attempted to use a method but the plugin hasn't been initialized yet."
                + " Be sure to use ModIOUnity.InitializeForUser to initialize the plugin "
                + "before attempting this method again (Or ModIOUnityAsync.InitializeForUser).");
            return false;
        }

        internal static async Task<Result> IsMarketplaceEnabled(bool silent = false)
        {
            if (await EnsureGameProfileHasBeenRetrieved())
            {
                if (GameProfile.HasValue && GameProfile.Value.monetization_options.HasFlag(GameMonetizationOptions.Enabled))
                {
                    return ResultBuilder.Success;
                }
                if (!silent)
                {
                    Logger.Log(
                        LogLevel.Error,
                        "Marketplace has not been enabled for this game profile. Ensure "
                        + "you have correctly setup the Marketplace feature on the mod.io website.");
                }
                return ResultBuilder.Create(ResultCode.Settings_MarketplaceNotEnabled);
            }
            // NOTE from STEVE:
            // If we fail to get the game profile it's likely we are offline and we simply dont know.
            // thus make sure not to rely on this method if the user just needs to use say GetPurchasedMods
            return ResultBuilder.Create(ResultCode.Settings_UnableToRetrieveGameProfile);
        }

        public static async Task<bool> EnsureGameProfileHasBeenRetrieved()
        {
            if (GameProfile.HasValue) return true;

            var response = await GetGameProfile();

            if (response.result.Succeeded())
            {
                GameProfile = response.value;
                return true;
            }

            Logger.Log(LogLevel.Error, "Unable to retrieve Game Profile from the server.");
            return false;
        }

        /// <summary>Checks the state of the credentials used to authenticate.</summary>
        public static bool IsAuthenticatedSessionValid(out Result result)
        {
            // Check if we have an Auth token saved to the current UserData
            if (UserData.instance == null || string.IsNullOrEmpty(UserData.instance.oAuthToken))
            {
                Logger.Log(
                    LogLevel.Verbose,
                    "The current session is not authenticated.");
                result = ResultBuilder.Create(ResultCode.User_NotAuthenticated);
                return false;
            }

            // Check if a previous WebRequest was rejected due to an old token
            if (UserData.instance.oAuthTokenWasRejected)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "The auth token was rejected. This could be because it's old and may"
                    + " need to be re-authenticated.");
                result = ResultBuilder.Create(ResultCode.User_InvalidToken);
                return false;
            }

            // No problems found, so therefore, it's probably still a valid session
            result = ResultBuilder.Success;
            return true;
        }

        /// <summary>
        /// This will check if a string has the correct layout for an email address. This doesn't
        /// check for a valid mailing server.
        /// </summary>
        /// <param name="emailaddress">string to check as a valid email</param>
        /// <param name="result">Result of the check</param>
        /// <returns>True if the string has a valid email address format</returns>
        public static bool IsValidEmail(string emailaddress, out Result result)
        {
            // MailAddress.TryCreate(emailaddress, out email); // <-- can't use this until .NET 6.0
            // Until .NET 6.0 we have to use a try-catch
            try
            {
                // Use System.Net.Mail.MailAddress' constructor to validate the email address string
                MailAddress email = new MailAddress(emailaddress);
            }
            catch
            {
                result = ResultBuilder.Create(ResultCode.User_InvalidEmailAddress);
                Logger.Log(
                    LogLevel.Error,
                    "The Email Address provided was not recognised by .NET as a valid Email Address.");
                return false;
            }

            result = ResultBuilder.Success;
            return true;
        }

        static bool IsSearchFilterValid(SearchFilter filter, out Result result)
        {
            if (filter == null)
            {
                Logger.Log(LogLevel.Error,
                    "The SearchFilter parameter cannot be null. Be sure to assign a "
                    + "valid SearchFilter object before using GetMods method.");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
                return false;
            }

            return filter.IsSearchFilterValid(out result);
        }

        public static bool IsRateLimited(out Result result)
        {
            throw new NotImplementedException();
        }

        public static bool AreSettingsValid(out Result result)
        {
            throw new NotImplementedException();
        }

        #endregion // Synchronous Requirement Checks - to detect early outs and failures

        #region Initialization and Maintenance

        /// <summary>Assigns the logging delegate the plugin uses to output log messages.</summary>
        public static void SetLoggingDelegate(LogMessageDelegate loggingDelegate)
        {
            Logger.SetLoggingDelegate(loggingDelegate);
        }

        /// <summary>Initializes the Plugin for the given settings. Loads the
        /// state of mods installed on the system as well as the set of mods the
        /// specified user has installed on this device.</summary>
        public static Result InitializeForUser(string userProfileIdentifier,
                                               ServerSettings serverSettings,
                                               BuildSettings buildSettings)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks_dictionary.Add(callbackConfirmation, null);

            // clean user profile identifier in case of filename usage
            userProfileIdentifier = IOUtil.CleanFileNameForInvalidCharacters(userProfileIdentifier);

            Settings.server = serverSettings;
            Settings.build = buildSettings;

            // - load data services -
            // NOTE(@jackson):
            //  The order of the data module loading is important on standalone platforms.
            //  The UserDataService must be loaded before the PersistentDataService to ensure we
            //  load a potential persistent directory override stored in the user's json file. A
            //  directory override will be loaded in to the BuildSettings.extData field.

            // TODO(@jackson): Handle errors
            var createUds = PlatformConfiguration.CreateUserDataService(userProfileIdentifier,
                serverSettings.gameId, buildSettings);

            DataStorage.user = createUds.value;

            // - load user data - user.json needs to be loaded before persistant data service
            var result = DataStorage.LoadUserData();

            ResultAnd<IPersistentDataService> createPds = PlatformConfiguration.CreatePersistentDataService(serverSettings.gameId,
                buildSettings);

            DataStorage.persistent = createPds.value;

            ResultAnd<ITempDataService> createTds = PlatformConfiguration.CreateTempDataService(serverSettings.gameId,
                buildSettings);

            DataStorage.temp = createTds.value;

            if (result.code == ResultCode.IO_FileDoesNotExist
                || result.code == ResultCode.IO_DirectoryDoesNotExist)
            {
                UserData.instance = new UserData();
                result = DataStorage.SaveUserData();
            }

            // TODO We need to have one line that invokes

            if (!result.Succeeded())
            {
                // TODO(@jackson): Prepare for public
                callbackConfirmation.SetResult(true);
                openCallbacks_dictionary.Remove(callbackConfirmation);
                return result;
            }

            Logger.Log(LogLevel.Verbose, "Loading Registry");
            // - load registry -
            result = ModCollectionManager.LoadRegistry();

            Logger.Log(LogLevel.Verbose, "Finished Loading Registry");
            openCallbacks_dictionary[callbackConfirmation] = null;

            // Set response cache size limit
            ResponseCache.maxCacheSize = buildSettings.requestCacheLimitKB * 1024;

            // If we fail to load the registry we simply create a new one. It may be corrupted
            // if(!result.Succeeded())
            // {
            //     callbackConfirmation.SetResult(true);
            //     openCallbacks.Remove(callbackConfirmation);
            //     return result;
            // }

            // - finalize -
            isInitialized = true;

            result = ResultBuilder.Success;
            callbackConfirmation.SetResult(true);
            openCallbacks_dictionary.Remove(callbackConfirmation);

            Logger.Log(LogLevel.Message, $"Initialized User[{userProfileIdentifier}]");

            return result;
        }

        /// <summary>Initializes the Plugin for the given settings. Loads the
        /// state of mods installed on the system as well as the set of mods the
        /// specified user has installed on this device.</summary>
        public static Result InitializeForUser(string userProfileIdentifier)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks_dictionary.Add(callbackConfirmation, null);

            ServerSettings serverSettings;
            BuildSettings buildSettings;

            Result result = SettingsAsset.TryLoad(out serverSettings, out buildSettings);

            if (result.Succeeded())
            {
                result = InitializeForUser(userProfileIdentifier, serverSettings, buildSettings);
            }

            callbackConfirmation.SetResult(true);
            openCallbacks_dictionary.Remove(callbackConfirmation);
            return result;
        }

        /// <summary>
        /// Cancels any running public operations, frees plugin resources, and invokes
        /// any pending callbacks with a cancelled result code.
        /// </summary>
        public static async Task Shutdown(Action shutdownComplete)
        {
            if (!IsInitialized(out Result _))
            {
                Logger.Log(LogLevel.Verbose, "ALREADY SHUTDOWN");
                return;
            }

            // This first block ensures we dont have conflicting shutdown operations
            // being called at the same time.
            if (shuttingDown && shutdownOperation != null)
            {
                Logger.Log(LogLevel.Verbose, "WAITING FOR SHUTDOWN ");
                await shutdownOperation;
            }
            else
            {
                Logger.Log(LogLevel.Verbose, "SHUTTING DOWN");

                try
                {
                    shuttingDown = true;

                    // This passthrough ensures we can properly check for ongoing shutdown
                    // operations (see the above block)
                    shutdownOperation = ShutdownTask();

                    await shutdownOperation;

                    await openCallbacks.ShutDown();

                    shutdownOperation = null;

                    shuttingDown = false;
                }
                catch (Exception e)
                {
                    shuttingDown = false;
                    Logger.Log(LogLevel.Error, $"Exception caught when shutting down plugin: {e.Message} - inner={e.InnerException?.Message} - stacktrace: {e.StackTrace}");
                }


                Logger.Log(LogLevel.Verbose, "FINISHED SHUTDOWN");
            }

            shutdownComplete?.Invoke();
        }

        /// <summary>
        /// This method contains all of the actions that need to be taken in order to properly
        /// shutdown the plugin and free up all resources.
        /// </summary>
        static async Task ShutdownTask()
        {
            await WebRequestManager.Shutdown();
            await ModManagement.ShutdownOperations();
            await WssHandler.Shutdown();

            isInitialized = false;
            UserData.instance = null;
            // Settings.server = default;
            // Settings.build = default;
            ResponseCache.ClearCache();
            ModCollectionManager.ClearRegistry();

            // get new instance of dictionary so it's thread safe
            Dictionary<TaskCompletionSource<bool>, Task> tasks =
                new Dictionary<TaskCompletionSource<bool>, Task>(openCallbacks_dictionary);

            // iterate over the tasks and await for non faulted callbacks to finish
            using (var enumerator = tasks.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Value != null && enumerator.Current.Value.IsFaulted)
                    {
                        Logger.Log(LogLevel.Error,
                            "An Unhandled Exception was thrown in"
                            + " an awaited task. The corresponding callback"
                            + " will never be invoked.");
                        if (openCallbacks_dictionary.ContainsKey(enumerator.Current.Key))
                        {
                            openCallbacks_dictionary.Remove(enumerator.Current.Key);
                        }
                    }
                    else
                    {
                        await enumerator.Current.Key.Task;
                    }
                }
            }

            Logger.Log(LogLevel.Verbose, "Shutdown main handlers");
        }

        #endregion // Initialization and Maintenance

        #region Authentication

        public static async Task<Result> IsAuthenticated()
        {
            var callbackConfirmation = openCallbacks.New();
            Result result = ResultBuilder.Unknown;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.GetAuthenticatedUser.Request();
                var task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<UserObject>(config));
                result = task.result;

                if (result.Succeeded())
                {
                    result = task.result;
                    UserData.instance.SetUserObject(task.value);
                }
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        public static async void IsAuthenticated(Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(LogLevel.Warning, "No callback was given to the IsAuthenticated method. "
                                             + "This method has been cancelled.");
                return;
            }

            Result result = await IsAuthenticated();
            callback?.Invoke(result);
        }

        public static async Task<Result> RequestEmailAuthToken(string emailaddress)
        {
            var callbackConfirmation = openCallbacks.New();

            if (IsInitialized(out var result) && IsValidEmail(emailaddress, out result))
            {
                var config = API.Requests.AuthenticateViaEmail.Request(emailaddress);
                result = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request(config));
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }


        public static async void RequestEmailAuthToken(string emailaddress, Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the RequestEmailAuthToken method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            var result = await RequestEmailAuthToken(emailaddress);
            callback?.Invoke(result);
        }

        public static async Task<Result> SubmitEmailSecurityCode(string securityCode)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks_dictionary.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result = ResultBuilder.Unknown;
            //-------------------------------------------------------------------------------------
            if (string.IsNullOrWhiteSpace(securityCode))
            {
                Logger.Log(
                    LogLevel.Warning,
                    "The security code provided is null. Be sure to use the 5 digit code"
                    + " sent to the specified email address when using RequestEmailAuthToken()");
                ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }
            else if (IsInitialized(out result))
            {
                //      Synchronous checks SUCCEEDED
                WebRequestConfig config = API.Requests.AuthenticateUser.InternalRequest(securityCode);

                Task<ResultAnd<AccessTokenObject>> task = WebRequestManager.Request<AccessTokenObject>(config);

                // We always cache the task while awaiting so we can check IsFaulted externally
                openCallbacks_dictionary[callbackConfirmation] = task;
                ResultAnd<AccessTokenObject> response = await task;
                openCallbacks_dictionary[callbackConfirmation] = null;

                result = response.result;

                if (result.Succeeded())
                {
                    // Server request SUCCEEDED

                    // Assign deserialized response as the token

                    // Set User Access Token
                    UserData.instance.SetOAuthToken(response.value, AuthenticationServiceProvider.None);

                    // Get and cache the current user
                    // (using empty delegate instead of null callback to avoid log and early-out)
                    // TODO @Steve Need to discuss
                    // I never want to use these methods publicly, only ever calling them through
                    // front-end ModIOUnity class. I have some thoughts on this (See trello card)
                    // We could create another impl. class that just does direct 1:1 (more or less)
                    // API calls and in this impl class we simply implement and use the results to
                    // handle the logs and responses we'd want to give the front end user (also
                    // helps to keep track fo what WE are calling and what the user might be
                    // calling, the following line of code is a perfect example of how we'd expect
                    // slightly different behaviour)
                    await GetCurrentUser(delegate
                    { });

                    // continue to invoke at the end of this method
                }
            }

            //callback?.Invoke(result);
            callbackConfirmation.SetResult(true);
            openCallbacks_dictionary.Remove(callbackConfirmation);

            return result;
        }

        public static async void SubmitEmailSecurityCode(string securityCode,
                                                         Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the RequestEmailAuthToken method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await SubmitEmailSecurityCode(securityCode);
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<TermsOfUse>> GetTermsOfUse()
        {
            var callbackConfirmation = openCallbacks.New();

            var config = API.Requests.GetTerms.Request();
            TermsOfUse termsOfUse = default(TermsOfUse);

            if (IsInitialized(out var result) && !ResponseCache.GetTermsFromCache(config.Url, out termsOfUse))
            {
                //hmm okay
                //lets call it without the open callbacks?
                var task = WebRequestManager.Request<TermsObject>(config);
                var response = await openCallbacks.Run(callbackConfirmation, task);
                result = response.result;

                if (result.Succeeded())
                {
                    termsOfUse = ResponseTranslator.ConvertTermsObjectToTermsOfUse(response.value);

                    // Add terms to cache
                    ResponseCache.AddTermsToCache(config.Url, termsOfUse);
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(result, termsOfUse);
        }

        public static async void GetTermsOfUse(Action<ResultAnd<TermsOfUse>> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetTermsOfUse method, any response "
                    + "returned from the server wont be used. This operation has been cancelled.");
                return;
            }

            ResultAnd<TermsOfUse> resultAndTermsOfUse = await GetTermsOfUse();
            callback?.Invoke(resultAndTermsOfUse);
        }

        public static async Task<Result> AuthenticateUser(
            string data, AuthenticationServiceProvider serviceProvider,
            string emailAddress, TermsHash? hash, string nonce,
            OculusDevice? device, string userId, PlayStationEnvironment environment)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks_dictionary.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            if (IsInitialized(out result)
                && (emailAddress == null || IsValidEmail(emailAddress, out result)))
            {
                //      Synchronous checks SUCCEEDED

                WebRequestConfig config = API.Requests.AuthenticateUser.ExternalRequest(
                    serviceProvider, data, hash, emailAddress, nonce, device, userId, environment);

                Task<ResultAnd<AccessTokenObject>> task = WebRequestManager.Request<AccessTokenObject>(config);

                // We always cache the task while awaiting so we can check IsFaulted externally
                openCallbacks_dictionary[callbackConfirmation] = task;
                ResultAnd<AccessTokenObject> response = await task;
                openCallbacks_dictionary[callbackConfirmation] = null;

                result = response.result;

                if (result.Succeeded())
                {
                    // Server request SUCCEEDED

                    // Set User Access Token
                    UserData.instance.SetOAuthToken(response.value, serviceProvider);

                    // TODO @Steve (see other example, same situation in email auth)
                    await GetCurrentUser(delegate
                    { });
                }
                else
                {
                    Settings.build.SetDefaultPortal();
                }
            }

            SetUserPortal(serviceProvider);
            callbackConfirmation.SetResult(true);
            openCallbacks_dictionary.Remove(callbackConfirmation);

            return result;
        }

        private static void SetUserPortal(AuthenticationServiceProvider serviceProvider)
        {
            switch (serviceProvider)
            {
                case AuthenticationServiceProvider.Epic:
                    Settings.build.userPortal = UserPortal.EpicGamesStore;
                    break;
                case AuthenticationServiceProvider.Discord:
                    Settings.build.userPortal = UserPortal.Discord;
                    break;
                case AuthenticationServiceProvider.Google:
                    Settings.build.userPortal = UserPortal.Google;
                    break;
                case AuthenticationServiceProvider.Itchio:
                    Settings.build.userPortal = UserPortal.itchio;
                    break;
                case AuthenticationServiceProvider.Oculus:
                    Settings.build.userPortal = UserPortal.Oculus;
                    break;
                case AuthenticationServiceProvider.Steam:
                    Settings.build.userPortal = UserPortal.Steam;
                    break;
                case AuthenticationServiceProvider.Switch:
                    Settings.build.userPortal = UserPortal.Nintendo;
                    break;
                case AuthenticationServiceProvider.Xbox:
                    Settings.build.userPortal = UserPortal.XboxLive;
                    break;
                case AuthenticationServiceProvider.PlayStation:
                    Settings.build.userPortal = UserPortal.PlayStationNetwork;
                    break;
                case AuthenticationServiceProvider.GOG:
                    Settings.build.userPortal = UserPortal.GOG;
                    break;
            }
        }

        public static async void AuthenticateUser(
            string data, AuthenticationServiceProvider serviceProvider,
            string emailAddress, TermsHash? hash, string nonce,
            OculusDevice? device, string userId,
            PlayStationEnvironment environment, Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the AuthenticateUser method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await AuthenticateUser(data, serviceProvider, emailAddress, hash, nonce, device, userId, environment);
            callback?.Invoke(result);
        }

        public static async void BeginWssAuthentication(Action<ResultAnd<ExternalAuthenticationToken>> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the BeginWssAuthentication method, any response "
                    + "returned from the server wont be used. This operation has been cancelled.");
                return;
            }

            var response = await BeginWssAuthentication();
            callback?.Invoke(response);
        }

        public static async Task<ResultAnd<ExternalAuthenticationToken>> BeginWssAuthentication()
        {
            var callbackConfirmation = openCallbacks.New();
            var result = await openCallbacks.Run(callbackConfirmation, Wss.Wss.BeginAuthenticationProcess());
            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        #endregion // Authentication

        #region Mod Browsing

        public static async Task<ResultAnd<TagCategory[]>> GetGameTags()
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;
            TagCategory[] tags = new TagCategory[0];

            if (IsInitialized(out result) && !ResponseCache.GetTagsFromCache(out tags))
            {
                var config = API.Requests.GetGameTags.Request();

                var task = await openCallbacks.Run(callbackConfirmation,
                    WebRequestManager.Request<API.Requests.GetGameTags.ResponseSchema>(config));

                result = task.result;
                if (result.Succeeded())
                {
                    tags = ResponseTranslator.ConvertGameTagOptionsObjectToTagCategories(task.value.data);
                    ResponseCache.AddTagsToCache(tags);
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(result, tags);
        }

        public static async void GetGameTags(Action<ResultAnd<TagCategory[]>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetGameTags method, any response "
                    + "returned from the server wont be used. This operation has been cancelled.");
                return;
            }
            ResultAnd<TagCategory[]> result = await GetGameTags();
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<ModPage>> GetMods(SearchFilter filter)
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;
            ModPage page = new ModPage();

            string unpaginatedURL = API.Requests.GetMods.UnpaginatedURL(filter);
            var offset = filter.pageIndex * filter.pageSize;

            if (IsInitialized(out result) && IsSearchFilterValid(filter, out result)
                                          && !ResponseCache.GetModsFromCache(unpaginatedURL, offset, filter.pageSize, out page))
            {
                var config = API.Requests.GetMods.RequestPaginated(filter);

                var task = await openCallbacks.Run(callbackConfirmation,
                    WebRequestManager.Request<API.Requests.GetMods.ResponseSchema>(config));

                result = task.result;

                if (result.Succeeded())
                {
                    page = ResponseTranslator.ConvertResponseSchemaToModPage(task.value, filter);

                    // Return the exact number of mods that were requested (not more)
                    if (page.modProfiles.Length > filter.pageSize)
                    {
                        Array.Copy(page.modProfiles, page.modProfiles, filter.pageSize);
                    }
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(result, page);
        }

        public static async void GetMods(SearchFilter filter, Action<ResultAnd<ModPage>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetMods method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            ResultAnd<ModPage> result = await GetMods(filter);
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<CommentPage>> GetModComments(ModId modId, SearchFilter filter)
        {
            var callbackConfirmation = openCallbacks.New();

            CommentPage page = new CommentPage();
            var config = API.Requests.GetModComments.RequestPaginated(modId, filter);

            if (IsInitialized(out Result result) && IsSearchFilterValid(filter, out result)
                                                 && !ResponseCache.GetModCommentsFromCache(config.Url, out page))
            {
                var task = await openCallbacks.Run(callbackConfirmation,
                    WebRequestManager.Request<API.Requests.GetModComments.ResponseSchema>(config));

                result = task.result;

                if (result.Succeeded())
                {
                    page = ResponseTranslator.ConvertModCommentObjectsToCommentPage(task.value);

                    // Add this response into the cache
                    ResponseCache.AddModCommentsToCache(config.Url, page);

                    // Return the exact number of comments that were requested (not more)
                    if (page.CommentObjects.Length > filter.pageSize)
                    {
                        Array.Copy(page.CommentObjects, page.CommentObjects, filter.pageSize);
                    }
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(result, page);
        }

        public static async void GetModComments(ModId modId, SearchFilter filter, Action<ResultAnd<CommentPage>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetModComments method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            ResultAnd<CommentPage> result = await GetModComments(modId, filter);
            callback?.Invoke(result);
        }

        public static async Task GetMod(long id, Action<ResultAnd<ModProfile>> callback)
        {
            if (callback == null)
                Logger.Log(LogLevel.Warning, "No callback was given to the GetMod method, any response returned from the server wont be used. This operation has been cancelled.");
            else
                callback(await GetMod(id));
        }

        public static async Task<ResultAnd<ModProfile>> GetMod(long id)
        {
            ModProfile profile = default;

            if (!IsInitialized(out Result result) || ResponseCache.GetModFromCache((ModId)id, out profile))
                return ResultAnd.Create(result, profile);

            ResultAnd<ModObject> resultModObject = await GetModObject(id);

            return ResultAnd.Create(
                resultModObject.result,
                resultModObject.result.Succeeded() && ResponseCache.GetModFromCache((ModId)id, out profile)
                    ? profile
                    : default
            );
        }

        public static async Task<ResultAnd<GameObject>> GetGameProfile()
        {
            TaskCompletionSource<bool> callbackConfirmation = openCallbacks.New();
            WebRequestConfig config = API.Requests.GetGame.Request();

            ResultAnd<GameObject> task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<GameObject>(config));

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(
                task.result,
                task.value
            );
        }

        internal static async Task<ResultAnd<ModProfile>> GetModSkipCache(long id, Action<ResultAnd<ModProfile>> callback = null)
        {
            ModId modId = new ModId(id);

            ResponseCache.ClearModFromCache(modId);
            ResultAnd<ModObject> resultModObject = await GetModObject(id);

            if (!resultModObject.result.Succeeded())
                return ResultAnd.Create(resultModObject.result, (ModProfile)default);

            if (ModCollectionManager.HasModCollectionEntry(modId))
                ModCollectionManager.UpdateModCollectionEntry(modId, resultModObject.value);

            ResultAnd<ModProfile> result = ResultAnd.Create(
                resultModObject.result,
                resultModObject.result.Succeeded() && ResponseCache.GetModFromCache(modId, out ModProfile profile)
                    ? profile
                    : default
            );

            callback?.Invoke(result);

            return result;
        }

        static async Task<ResultAnd<ModObject>> GetModObject(long id)
        {
            TaskCompletionSource<bool> callbackConfirmation = openCallbacks.New();
            WebRequestConfig config = API.Requests.GetMod.Request(id);

            ResultAnd<ModObject> task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<ModObject>(config));

            openCallbacks.Complete(callbackConfirmation);

            if (task.result.Succeeded())
                ResponseCache.AddModToCache(
                    ResponseTranslator.ConvertModObjectToModProfile(task.value)
                );

            return task;
        }

        public static async Task<ResultAnd<ModDependencies[]>> GetModDependencies(ModId modId)
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;
            ModDependencies[] modDependencies = default;

            if (IsInitialized(out result) && !ResponseCache.GetModDependenciesCache(modId, out modDependencies))
            {
                var config = API.Requests.GetModDependencies.Request(modId);
                var task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<API.Requests.GetModDependencies.ResponseSchema>(config));

                result = task.result;

                if (result.Succeeded())
                {
                    modDependencies = ResponseTranslator.ConvertModDependenciesObjectToModDependencies(task.value.data);
                    ResponseCache.AddModDependenciesToCache(modId, modDependencies);
                }
            }

            openCallbacks.Complete(callbackConfirmation);
            return ResultAnd.Create(result, modDependencies);
        }

        public static async void GetModDependencies(ModId modId, Action<ResultAnd<ModDependencies[]>> callback)
        {
            // Check for callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetModDependencies method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            var result = await GetModDependencies(modId);
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<Rating[]>> GetCurrentUserRatings()
        {
            var callbackConfirmation = openCallbacks.New();

            Result result = default;
            Rating[] ratings = default;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result)
                                          && !ResponseCache.GetCurrentUserRatingsCache(out ratings))
            {
                var config = API.Requests.GetCurrentUserRatings.Request();
                var task = ModCollectionManager.TryRequestAllResults<RatingObject>(config.Url, API.Requests.GetCurrentUserRatings.Request);
                var response = await openCallbacks.Run(callbackConfirmation, task);

                result = response.result;

                if (result.Succeeded())
                {
                    ratings = ResponseTranslator.ConvertModRatingsObjectToRatings(response.value);

                    ResponseCache.ReplaceCurrentUserRatings(ratings);
                }
            }

            // FINAL SUCCESS / FAILURE depending on callback params set previously
            openCallbacks.Complete(callbackConfirmation);
            return ResultAnd.Create(result, ratings);
        }

        public static async void GetCurrentUserRatings(Action<ResultAnd<Rating[]>> callback)
        {
            // Check for callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetCurrentUserRatings method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            var result = await GetCurrentUserRatings();
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<ModRating>> GetCurrentUserRatingFor(ModId modId)
        {
            var callbackConfirmation = openCallbacks.New();

            //------------------------------[ Setup callback params ]------------------------------
            Result result = ResultBuilder.Unknown;
            ModRating rating = default;
            //-------------------------------------------------------------------------------------

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                // If the ratings haven't been cached this session, we can do so here
                if (!ResponseCache.HaveRatingsBeenCachedThisSession())
                {
                    // If there is no rating, make sure we've cached the ratings
                    Task<ResultAnd<Rating[]>> task = GetCurrentUserRatings();
                    ResultAnd<Rating[]> response = await openCallbacks.Run(callbackConfirmation, task);

                    if (!response.result.Succeeded())
                    {
                        result = response.result;
                        goto End;
                    }
                }

                // Try to get a single rating from the cache
                if (ResponseCache.GetCurrentUserRatingFromCache(modId, out rating))
                {
                    result = ResultBuilder.Success;
                }
            }

        End:

            // FINAL SUCCESS / FAILURE depending on callback params set previously
            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return ResultAnd.Create(result, rating);
        }

        public static async void GetCurrentUserRatingFor(ModId modId, Action<ResultAnd<ModRating>> callback)
        {
            // Check for callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetCurrentUserRatingFor method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            var result = await GetCurrentUserRatingFor(modId);
            callback?.Invoke(result);
        }

        #endregion // Mod Browsing

        #region Mod Management

        public static Result EnableModManagement(
            ModManagementEventDelegate modManagementEventDelegate)
        {
            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                ModManagement.modManagementEventDelegate = modManagementEventDelegate;
                ModManagement.EnableModManagement();
            }

            return result;
        }
#pragma warning disable 4014
        public static Result DisableModManagement()
        {
            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                ModManagement.DisableModManagement();

                ModManagement.ShutdownOperations();
            }

            return result;
        }
#pragma warning restore 4014

        public static async Task<Result> FetchUpdates()
        {
            var callbackConfirmation = openCallbacks.New();

            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                result = await openCallbacks.Run(callbackConfirmation, ModCollectionManager.FetchUpdates());

                if (result.Succeeded())
                {
                    ModManagement.WakeUp();
                }
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        public static async Task FetchUpdates(Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(LogLevel.Warning,
                    "No callback was given for the FetchUpdates"
                    + " method. This is not recommended because you will "
                    + "not know if the fetch was successful.");
            }

            Result result = await FetchUpdates();
            callback?.Invoke(result);
        }

        // This is technically redundant (See how it's implemented), consider removing.
        public static bool IsModManagementBusy()
        {
            return ModManagement.GetCurrentOperationProgress() != null;
        }

        public static Result ForceUninstallMod(ModId modId)
        {
            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                result =
                    ModCollectionManager.MarkModForUninstallIfNotSubscribedToCurrentSession(modId);
                ModManagement.WakeUp();
            }

            return result;
        }

        public static ProgressHandle GetCurrentModManagementOperation()
        {
            return ModManagement.GetCurrentOperationProgress();
        }

        public static bool EnableMod(ModId modId)
        {
            if (!IsInitialized(out Result _))
            {
                return false;
            }

            return ModCollectionManager.EnableModForCurrentUser(modId);
        }

        public static bool DisableMod(ModId modId)
        {
            if (!IsInitialized(out Result _))
            {
                return false;
            }

            return ModCollectionManager.DisableModForCurrentUser(modId);
        }

        public static async void AddDependenciesToMod(ModId modId, ICollection<ModId> dependencies, Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the AddDependenciesToMod method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await AddDependenciesToMod(modId, dependencies);
            callback?.Invoke(result);
        }

        public static async Task<Result> AddDependenciesToMod(ModId modId, ICollection<ModId> dependencies)
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;

            if (dependencies.Count > 5)
            {
                result = ResultBuilder.Create(ResultCode.InvalidParameter_TooMany);
                Logger.Log(
                    LogLevel.Warning,
                    "You can only change a maximum of 5 dependencies in a single request."
                    + " If you need to add more than 5 dependencies consider doing it over "
                    + "multiple requests instead.");
            }
            else if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.AddDependency.Request(modId, dependencies);
                result = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request(config));

                if (result.Succeeded())
                {
                    // TODO update cache for this mod's dependencies
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        public static async void RemoveDependenciesFromMod(ModId modId, ICollection<ModId> dependencies, Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the RemoveDependenciesFromMod method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await RemoveDependenciesFromMod(modId, dependencies);
            callback?.Invoke(result);
        }

        public static async Task<Result> RemoveDependenciesFromMod(ModId modId, ICollection<ModId> dependencies)
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;

            if (dependencies.Count > 5)
            {
                result = ResultBuilder.Create(ResultCode.InvalidParameter_TooMany);
                Logger.Log(
                    LogLevel.Warning,
                    "You can only change a maximum of 5 dependencies in a single request."
                    + " If you need to remove more than 5 dependencies consider doing it over "
                    + "multiple requests instead.");
            }
            else if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.DeleteDependency.Request(modId, dependencies);
                result = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request(config));

                if (result.Succeeded())
                {
                    // TODO update cache for this mod's dependencies
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        #endregion // Mod Management

        #region User Management

        public static async Task<Result> AddModRating(ModId modId, ModRating modRating)
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {

                var config = API.Requests.AddModRating.Request(modId, modRating);
                var response = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<MessageObject>(config));

                result = response.result;

                var rating = new Rating
                {
                    dateAdded = DateTime.Now,
                    rating = modRating,
                    modId = modId
                };
                ResponseCache.AddCurrentUserRating(modId, rating);

                if (result.code_api == ResultCode.RESTAPI_ModRatingAlreadyExists
                    || result.code_api == ResultCode.RESTAPI_ModRatingNotFound)
                {
                    // SUCCEEDED
                    result = ResultBuilder.Success;
                }
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }


        public static async void AddModRating(ModId modId, ModRating rating,
                                              Action<Result> callback)
        {
            // Callback warning
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the AddModRating method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await AddModRating(modId, rating);
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<UserProfile>> GetCurrentUser()
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;
            UserProfile userProfile = default;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result)
                                          && !ResponseCache.GetUserProfileFromCache(out userProfile))
            {
                var config = API.Requests.GetAuthenticatedUser.Request();
                var task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<UserObject>(config));

                result = task.result;

                if (result.Succeeded())
                {
                    UserData.instance.SetUserObject(task.value);
                    userProfile = ResponseTranslator.ConvertUserObjectToUserProfile(task.value);

                    // Add UserProfile to cache (lasts for the whole session)
                    ResponseCache.AddUserToCache(userProfile);
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks_dictionary.Remove(callbackConfirmation);

            return ResultAnd.Create(result, userProfile);
        }

        public static async Task GetCurrentUser(Action<ResultAnd<UserProfile>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetCurrentUser method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }

            var result = await GetCurrentUser();
            callback(result);
        }

        public static async Task<Result> UnsubscribeFrom(ModId modId)
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.UnsubscribeFromMod.Request(modId);
                var task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<MessageObject>(config));

                result = task.result;
                var success = result.Succeeded()
                              || result.code_api == ResultCode.RESTAPI_ModSubscriptionNotFound;

                if (success)
                {
                    result = ResultBuilder.Success;
                    ModCollectionManager.RemoveModFromUserSubscriptions(modId, false);

                    if (ShouldAbortDueToDownloading(modId))
                    {
                        ModManagement.AbortCurrentDownloadJob();
                    }
                    else if (ShouldAbortDueToInstalling(modId))
                    {
                        ModManagement.AbortCurrentInstallJob();
                    }
                    ModManagement.WakeUp();
                }

                ModCollectionManager.RemoveModFromUserSubscriptions(modId, success);
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        static bool ShouldAbortDueToDownloading(ModId modId)
        {
            return ModManagement.currentJob != null
                   && ModManagement.currentJob.modEntry.modObject.id == modId
                   && ModManagement.currentJob.type == ModManagementOperationType.Download;
        }

        static bool ShouldAbortDueToInstalling(ModId modId)
        {
            return ModManagement.currentJob != null
                   && ModManagement.currentJob.modEntry.modObject.id == modId
                   && ModManagement.currentJob.type == ModManagementOperationType.Install
                   && ModManagement.currentJob.zipOperation != null;
        }

        public static async void UnsubscribeFrom(ModId modId, Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the UnsubscribeFrom method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await UnsubscribeFrom(modId);
            callback?.Invoke(result);
        }

        public static async Task<Result> SubscribeTo(ModId modId)
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.SubscribeToMod.Request(modId);
                var taskResult = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<ModObject>(config));

                result = taskResult.result;

                if (result.Succeeded())
                {
                    ModCollectionManager.UpdateModCollectionEntry(modId, taskResult.value);
                    ModCollectionManager.AddModToUserSubscriptions(modId);
                    ModManagement.WakeUp();
                }
                else if (result.code_api == ResultCode.RESTAPI_ModSubscriptionAlreadyExists)
                {
                    // Hack implementation:
                    // If sub exists, then we don't receive the Mod Object
                    // So, our sub request did nothing.
                    // If the we attempt to fetch the Mod Object, and it fails,
                    // treat the subscribe attempt as a failure.

                    ModCollectionManager.AddModToUserSubscriptions(modId);

                    var getModConfig = API.Requests.GetMod.Request(modId);
                    var getModConfigResult = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<ModObject>(getModConfig));

                    if (getModConfigResult.result.Succeeded())
                    {
                        ModCollectionManager.UpdateModCollectionEntry(modId, getModConfigResult.value);
                        ModManagement.WakeUp();
                    }

                    result = getModConfigResult.result;
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        public static async void SubscribeTo(ModId modId, Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the SubscribeTo method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await SubscribeTo(modId);
            callback?.Invoke(result);
        }


        //Should this be exposed in ModIOUnity/ModIOUnityAsync?
        public static async Task<ResultAnd<ModPage>> GetUserSubscriptions(SearchFilter filter)
        {
            var callbackConfirmation = openCallbacks.New();
            Result result;
            ModPage page = new ModPage();

            if (IsInitialized(out result) && IsSearchFilterValid(filter, out result)
                                          && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.GetUserSubscriptions.Request(filter);
                var task = await openCallbacks.Run(callbackConfirmation,
                    WebRequestManager.Request<API.Requests.GetUserSubscriptions.ResponseSchema>(config));

                result = task.result;

                if (result.Succeeded())
                {
                    page = ResponseTranslator.ConvertResponseSchemaToModPage(task.value, filter);
                }
            }

            openCallbacks.Complete(callbackConfirmation);
            return ResultAnd.Create(result, page);
        }

        public static ModProfile[] GetPurchasedMods(out Result result)
        {
            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                return ModCollectionManager.GetPurchasedMods(out result);
            }

            return null;
        }

        public static SubscribedMod[] GetSubscribedMods(out Result result)
        {
            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                SubscribedMod[] mods = ModCollectionManager.GetSubscribedModsForUser(out result);
                return mods;
            }

            return null;
        }

        public static InstalledMod[] GetInstalledMods(out Result result)
        {
            if (IsInitialized(out result) /* && AreCredentialsValid(false, out result)*/)
            {
                InstalledMod[] mods = ModCollectionManager.GetInstalledMods(out result, true);
                return mods;
            }

            return null;
        }

        public static UserInstalledMod[] GetInstalledModsForUser(out Result result, bool includeDisabledMods)
        {
            //Filter for user
            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var mods = ModCollectionManager.GetInstalledMods(out result, false);
                return FilterInstalledModsIntoUserInstalledMods(UserData.instance.userObject.id, includeDisabledMods, mods);
            }

            return null;
        }

        internal static UserInstalledMod[] FilterInstalledModsIntoUserInstalledMods(long userId, bool includeDisabledMods, params InstalledMod[] mods)
            => mods.Select(x => x.AsInstalledModsUser(userId))
                .Where(x => !x.Equals(default(UserInstalledMod)))
                .Where(x => x.enabled || includeDisabledMods)
                .ToArray();

        public static Result RemoveUserData()
        {
            // We do not need to await this MM shutdown, it can happen silently
#pragma warning disable
            ModManagement.ShutdownOperations();
#pragma warning restore

            DisableModManagement();

            // remove the user from mod collection registry of subscribed mods
            ModCollectionManager.ClearUserData();

            // remove the user's auth token and credentials, clear the session
            UserData.instance?.ClearUser();

            // clear the UserProfile from the cache as it is no longer valid
            ResponseCache.ClearUserFromCache();

            bool userExists = ModCollectionManager.DoesUserExist();

            Result result = userExists
                ? ResultBuilder.Create(ResultCode.User_NotRemoved)
                : ResultBuilder.Success;

            return result;
        }

        public static async Task<Result> DownloadNow(ModId modId)
        {
            return await ModManagement.DownloadNow(modId);
        }

        public static async void DownloadNow(ModId modId, Action<Result> callback)
        {
            Result result = await DownloadNow(modId);
            callback?.Invoke(result);
        }

        public static async void MuteUser(long userId, Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the MuteUser method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await MuteUser(userId);
            callback?.Invoke(result);
        }


        public static async void UnmuteUser(long userId, Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the UnmuteUser method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await UnmuteUser(userId);
            callback?.Invoke(result);
        }

        public static async void GetMutedUsers(Action<ResultAnd<UserProfile[]>> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetMutedUsers method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            ResultAnd<UserProfile[]> result = await GetMutedUsers();
            callback?.Invoke(result);
        }

        public static async Task<Result> MuteUser(long userId)
        {
            var callbackConfirmation = openCallbacks.New();
            Result result;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.UserMute.Request(userId);
                var task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request(config));
                result = task;
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        public static async Task<Result> UnmuteUser(long userId)
        {
            var callbackConfirmation = openCallbacks.New();
            Result result;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.UserUnmute.Request(userId);
                var task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request(config));
                result = task;
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        public static async Task<ResultAnd<UserProfile[]>> GetMutedUsers()
        {
            var callbackConfirmation = openCallbacks.New();
            ResultAnd<UserProfile[]> response = new ResultAnd<UserProfile[]>();

            if (IsInitialized(out response.result) && IsAuthenticatedSessionValid(out response.result))
            {
                ResultAnd<UserObject[]> getMutedUsers = await openCallbacks.Run(callbackConfirmation, ModCollectionManager.TryRequestAllResults<UserObject>(API.Requests.GetMutedUsers.Url, API.Requests.GetMutedUsers.Request));
                response.result = getMutedUsers.result;

                if (getMutedUsers.result.Succeeded())
                {
                    response.value = getMutedUsers.value.Select(ResponseTranslator.ConvertUserObjectToUserProfile).ToArray();
                }
            }

            openCallbacks.Complete(callbackConfirmation);
            return response;
        }

        #endregion // User Management

        #region Mod Media

#if UNITY_2019_4_OR_NEWER
        public static async Task<ResultAnd<Texture2D>> DownloadTexture(DownloadReference downloadReference)
        {
            //------------------------------[ Setup callback params ]------------------------------
            Result result;
            Texture2D texture = null;
            //-------------------------------------------------------------------------------------

            ResultAnd<byte[]> resultAnd = await GetImage(downloadReference);
            result = resultAnd.result;

            if (result.Succeeded())
            {
                IOUtil.TryParseImageData(resultAnd.value, out texture, out result);
            }

            return ResultAnd.Create(result, texture);
        }
#endif
        /// <summary>
        /// This will first check if we are already attempting to download the same image with a
        /// different web request. Instead of competing for a file stream it will simply wait for
        /// the result of the other duplicate request (if any)
        /// </summary>
        public static async Task<ResultAnd<byte[]>> GetImage(DownloadReference downloadReference)
        {
            if (!downloadReference.IsValid())
            {
                Logger.Log(
                    LogLevel.Warning,
                    "The DownloadReference provided for the DownloadImage method was not"
                    + " valid. Consider using the DownloadReference.IsValid() method to check if the"
                    + "DownloadReference has an existing URL before using this method.");
                return ResultAnd.Create<byte[]>(ResultCode.InvalidParameter_DownloadReferenceIsntValid, null);
            }
            if (onGoingImageDownloads.ContainsKey(downloadReference.url))
            {
                Logger.Log(LogLevel.Verbose, $"The image ({downloadReference.filename}) "
                                             + $"is already being download. Waiting for duplicate request's result.");
                return await onGoingImageDownloads[downloadReference.url];
            }

            var task = DownloadImage(downloadReference);
            onGoingImageDownloads.Add(downloadReference.url, task);
            var result = await task;
            onGoingImageDownloads.Remove(downloadReference.url);
            return result;
        }

        static async Task<ResultAnd<byte[]>> DownloadImage(DownloadReference downloadReference)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks_dictionary.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback params ]------------------------------
            Result result;
            byte[] image = null;
            //-------------------------------------------------------------------------------------

            if (IsInitialized(out result))
            {
                // Check cache asynchronously for texture in temp folder
                Task<ResultAnd<byte[]>> cacheTask =
                    ResponseCache.GetImageFromCache(downloadReference);

                openCallbacks_dictionary[callbackConfirmation] = cacheTask;
                ResultAnd<byte[]> cacheResponse = await cacheTask;
                openCallbacks_dictionary[callbackConfirmation] = null;
                result = cacheResponse.result;

                if (result.Succeeded())
                {
                    // CACHE SUCCEEDED
                    result = cacheResponse.result;
                    image = cacheResponse.value;
                }
                else
                {
                    // GET FILE STREAM TO DOWNLOAD THE IMAGE FILE TO
                    // This stream is a direct write to the file location we will cache the
                    // image to so we dont need to add the image to cache once we're done so to speak
                    ResultAnd<ModIOFileStream> openWriteStream = DataStorage.GetImageFileWriteStream(downloadReference.url);
                    result = openWriteStream.result;

                    if (result.Succeeded())
                    {
                        using (openWriteStream.value)
                        {
                            // DOWNLOAD THE IMAGE
                            var handle = WebRequestManager.Download(downloadReference.url, openWriteStream.value, null);
                            result = await handle.task;
                        }

                        if (result.Succeeded())
                        {
                            // We need to re-open the stream because some platforms only allow a Read or Write stream, not both
                            ResultAnd<ModIOFileStream> openReadStream = DataStorage.GetImageFileReadStream(downloadReference.url);
                            result = openReadStream.result;

                            if (result.Succeeded())
                            {
                                using (openReadStream.value)
                                {
                                    var readAllBytes = await openReadStream.value.ReadAllBytesAsync();
                                    result = readAllBytes.result;

                                    if (result.Succeeded())
                                    {
                                        // CACHE SUCCEEDED
                                        image = readAllBytes.value;
                                    }
                                }
                            }
                        }

                        // FAILED DOWNLOAD - ERASE THE FILE SO WE DONT CREATE A CORRUPT CACHED IMAGE
                        if (!result.Succeeded())
                        {
                            Result cleanupResult = DataStorage.DeleteStoredImage(downloadReference.url);
                            if (!cleanupResult.Succeeded())
                            {
                                Logger.Log(LogLevel.Error,
                                    $"[Internal] Failed to cleanup downloaded image."
                                    + $" This may result in a corrupt or invalid image being"
                                    + $" loaded for modId {downloadReference.modId}");
                            }
                        }
                    }
                }
                // continue to invoke at the end of this method
            }


            callbackConfirmation.SetResult(true);
            openCallbacks_dictionary.Remove(callbackConfirmation);

            return ResultAnd.Create(result, image);
        }

#if UNITY_2019_4_OR_NEWER
        public static async void DownloadTexture(DownloadReference downloadReference,
                                                 Action<ResultAnd<Texture2D>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the DownloadTexture method. This operation has been cancelled.");
                return;
            }
            if (!IsInitialized(out Result initResult))
            {
                var r = ResultAnd.Create<Texture2D>(initResult, null);
                callback?.Invoke(r);
                return;
            }

            ResultAnd<Texture2D> result = await DownloadTexture(downloadReference);
            callback?.Invoke(result);
        }
#endif
        public static async void DownloadImage(DownloadReference downloadReference,
                                               Action<ResultAnd<byte[]>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the DownloadImage method. This operation has been cancelled.");
                return;
            }
            if (!IsInitialized(out Result initResult))
            {
                var r = ResultAnd.Create<byte[]>(initResult, null);
                callback?.Invoke(r);
                return;
            }

            ResultAnd<byte[]> result = await GetImage(downloadReference);
            callback?.Invoke(result);
        }

        #endregion // Mod Media

        #region Reporting

        public static async Task<Result> Report(Report report)
        {
            var callbackConfirmation = openCallbacks.New();
            Result result = ResultBuilder.Unknown;

            if (report == null || !report.CanSend())
            {
                Logger.Log(LogLevel.Error,
                    "The Report instance provided to the Reporting method is not setup correctly"
                    + " and cannot be sent as a valid report to mod.io");

                result = report == null
                    ? ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull)
                    : ResultBuilder.Create(ResultCode.InvalidParameter_ReportNotReady);
            }
            else if (IsInitialized(out result))
            {
                var config = API.Requests.Report.Request(report);
                var task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<MessageObject>(config));
                result = task.result;
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        public static async void Report(Report report, Action<Result> callback)
        {
            // TODO @Steve implement reporting for users
            // This has to be done before GDK and XDK implementation is publicly supported

            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the Report method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await Report(report);
            callback?.Invoke(result);
        }

        #endregion // Reporting

        #region Mod Uploading

        public static CreationToken GenerateCreationToken()
        {
            return ModManagement.GenerateNewCreationToken();
        }


        public static async Task<ResultAnd<ModId>> CreateModProfile(CreationToken token, ModProfileDetails modDetails)
        {
            // - Early Outs -
            if (Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error,
                    "The current plugin configuration has uploading disabled.");

                return ResultAnd.Create(ResultBuilder.Create(ResultCode.Settings_UploadsDisabled), ModId.Null);
            }

            var callbackConfirmation = openCallbacks.New();

            Result result;
            ModId modId = (ModId)0;

            if (!ModManagement.IsCreationTokenValid(token))
            {
                Logger.Log(
                    LogLevel.Error,
                    "The provided CreationToken is not valid and cannot be used to create "
                    + "a new mod profile. Be sure to use GenerateCreationToken() before attempting to"
                    + " create a new Mod Profile");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_BadCreationToken);
            }
            else
            {
                if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result)
                                              && IsModProfileDetailsValid(modDetails, out result))
                {
                    var config = API.Requests.AddMod.Request(modDetails);
                    var response = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<ModObject>(config));
                    result = response.result;

                    if (result.Succeeded())
                    {
                        modId = (ModId)response.value.id;

                        ModManagement.InvalidateCreationToken(token);
                        ResponseCache.ClearCache();

                        modDetails.modId = (ModId)response.value.id;
                        result = await ValidateModProfileMarketplaceTeam(modDetails);
                    }
                }
            }

            openCallbacks.Complete(callbackConfirmation);
            return ResultAnd.Create(result, modId);
        }

        public static async void CreateModProfile(CreationToken token, ModProfileDetails modDetails,
                                                  Action<ResultAnd<ModId>> callback)
        {
            // - Early Outs -
            // Check callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Error,
                    "No callback was given to the CreateModProfile method. You need"
                    + "to retain the ModId returned by the callback in order to further apply changes"
                    + "or edits to the newly created mod profile. The operation has been cancelled.");
                return;
            }

            var result = await CreateModProfile(token, modDetails);
            callback?.Invoke(result);
        }

        public static async Task<Result> EditModProfile(ModProfileDetails modDetails)
        {
            // - Early Outs -
            // Check disableUploads
            if (Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error,
                    "The current plugin configuration has uploading disabled.");

                return ResultBuilder.Create(ResultCode.Settings_UploadsDisabled);
            }

            var callbackConfirmation = openCallbacks.New();
            Result result;

            // Check for modId
            if (modDetails == null)
            {
                Logger.Log(LogLevel.Error,
                    "The ModProfileDetails provided is null. You cannot update a mod "
                    + "without providing a valid ModProfileDetails object.");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }
            else if (modDetails.modId == null)
            {
                Logger.Log(LogLevel.Error,
                    "The provided ModProfileDetails has not been assigned a ModId. Ensure"
                    + " you assign the Id of the mod you intend to edit to the ModProfileDetails.modId"
                    + " field.");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_ModProfileRequiredFieldsNotSet);
            }
            else if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result)
                                               && IsModProfileDetailsValidForEdit(modDetails, out result))
            {
                // TODO remove this warning if the EditMod endpoint adds tag editing feature
                if (modDetails.tags != null && modDetails.tags.Length > 0)
                {
                    Logger.Log(LogLevel.Warning,
                        "The EditMod method cannot be used to change a ModProfile's tags."
                        + " Use the ModIOUnity.AddTags and ModIOUnity.DeleteTags methods instead."
                        + " The 'tags' array in the ModProfileDetails will be ignored.");
                }

                result = await ValidateModProfileMarketplaceTeam(modDetails);

                var config = modDetails.logo != null
                    ? API.Requests.EditMod.RequestPOST(modDetails)
                    : API.Requests.EditMod.RequestPUT(modDetails);

                result = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request(config));

                if (result.Succeeded())
                {
                    // TODO This request returns the new ModObject, we should cache this new mod profile when we succeed
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }


        public static async void EditModProfile(ModProfileDetails modDetails,
                                                Action<Result> callback)
        {
            // Check callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the EditModProfile method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            var result = await EditModProfile(modDetails);
            callback?.Invoke(result);
        }

        /// <summary>
        /// If a user attempts to create or edit a mod's monetization options to 'Live',
        /// this method will ensure the monetization team and revenue split is set correctly
        /// by setting the existing user's revenue share to 100%.
        /// </summary>
        /// <remarks>
        /// This first attempts to GET the monetization team before setting it. The reason we dont
        /// just force it to always be the existing user with 100% revenue share is because the
        /// revenue split can be edited and set with multiple users elsewhere.
        /// (It's rare but possible, and we dont want to override an existing team)
        /// </remarks>
        /// <param name="modDetails"></param>
        /// <returns></returns>
        static async Task<Result> ValidateModProfileMarketplaceTeam(ModProfileDetails modDetails)
        {
            // If not setting monetization to live, we don't need to validate that a marketplace team has been setup
            if (modDetails.monetizationOptions.HasValue && modDetails.monetizationOptions.Value.HasFlag(MonetizationOption.Live))
            {
                return ResultBuilder.Success;
            }

            Result result = ResultBuilder.Unknown;

            if (modDetails.modId == null)
            {
                return result;
            }

            var getResponse = await GetModMonetizationTeam(modDetails.modId.Value);

            if (getResponse.result.Succeeded())
            {
                return ResultBuilder.Success;
            }

            List<ModMonetizationTeamDetails> team = new List<ModMonetizationTeamDetails>
            {
                new ModMonetizationTeamDetails(UserData.instance.userObject.id, 100)
            };
            result = await AddModMonetizationTeam(modDetails.modId.Value, team);

            return result;
        }

        public static async void DeleteTags(ModId modId, string[] tags,
                                            Action<Result> callback)
        {
            // Check callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the DeleteTags method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            var result = await DeleteTags(modId, tags);
            callback?.Invoke(result);
        }

        public static async Task<Result> DeleteTags(ModId modId, string[] tags)
        {
            // - Early Outs -
            // Check disableUploads
            if (Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error,
                    "The current plugin configuration has uploading disabled.");

                return ResultBuilder.Create(ResultCode.Settings_UploadsDisabled);
            }

            var callbackConfirmation = openCallbacks.New();
            Result result;

            if (modId == 0)
            {
                Logger.Log(LogLevel.Error, "You must provide a valid mod id to delete tags.");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_MissingModId);
            }
            else if (tags == null || tags.Length == 0)
            {
                Logger.Log(LogLevel.Error, "You must provide tags to be deleted from the mod");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }
            else if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.DeleteModTags.Request(modId, tags);
                var taskResult = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<MessageObject>(config));
                result = taskResult.result;
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        public static async Task<ResultAnd<ModComment>> AddModComment(ModId modId, CommentDetails commentDetails)
        {
            var callbackConfirmation = openCallbacks.New();
            ModComment comment = default;

            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.AddModComment.Request(modId, commentDetails);
                var taskResult = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<ModCommentObject>(config));
                result = taskResult.result;
                comment = ResponseTranslator.ConvertModCommentObjectsToModComment(taskResult.value);
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(result, comment);
        }

        public static async void AddModComment(ModId modId, CommentDetails commentDetails, Action<ResultAnd<ModComment>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the AddModComment method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            ResultAnd<ModComment> result = await AddModComment(modId, commentDetails);
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<ModComment>> UpdateModComment(ModId modId, string content, long commentId)
        {
            var callbackConfirmation = openCallbacks.New();
            ModComment comment = default;

            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.UpdateModComment.Request(modId, content, commentId);
                var taskResult = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<ModCommentObject>(config));
                result = taskResult.result;
                comment = ResponseTranslator.ConvertModCommentObjectsToModComment(taskResult.value);
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(result, comment);
        }

        public static async void UpdateModComment(ModId modId, string content, long commentId, Action<ResultAnd<ModComment>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the UpdateModComment method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            ResultAnd<ModComment> result = await UpdateModComment(modId, content, commentId);
            callback?.Invoke(result);
        }

        public static async Task<Result> DeleteModComment(ModId modId, long commentId)
        {
            var callbackConfirmation = openCallbacks.New();
            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.DeleteModComment.Request(modId, commentId);
                var taskResult = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<ModCommentObject>(config));
                result = taskResult.result;
                if (result.Succeeded())
                {
                    ResponseCache.RemoveModCommentFromCache(commentId);
                }
            }
            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        public static async void DeleteModComment(ModId modId, long commentId, Action<Result> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the DeleteModComment method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            Result result = await DeleteModComment(modId, commentId);
            callback?.Invoke(result);
        }

        public static async void AddTags(ModId modId, string[] tags,
                                         Action<Result> callback)
        {
            // Check callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the AddTags method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            var result = await AddTags(modId, tags);
            callback?.Invoke(result);
        }

        public static async Task<Result> AddTags(ModId modId, string[] tags)
        {
            // - Early Outs -
            // Check disableUploads
            if (Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error, "The current plugin configuration has uploading disabled.");
                return ResultBuilder.Create(ResultCode.Settings_UploadsDisabled);
            }

            var callbackConfirmation = openCallbacks.New();
            Result result;

            // Check for modId
            if (modId == 0)
            {
                Logger.Log(LogLevel.Error, "You must provide a valid mod id to add tags.");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_MissingModId);
            }
            else if (tags == null || tags.Length == 0)
            {
                Logger.Log(LogLevel.Error, "You must provide tags to be added to the mod");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }
            else if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.AddModTags.Request(modId, tags);
                var taskResult = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<MessageObject>(config));
                result = taskResult.result;
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        public static ProgressHandle GetCurrentUploadHandle()
        {
            return currentUploadHandle;
        }

        public static async Task<Result> UploadModMedia(ModProfileDetails modProfileDetails)
        {
            // - Early outs -
            // Check Modfile
            if (modProfileDetails == null)
            {
                Logger.Log(LogLevel.Error, "ModfileDetails parameter cannot be null.");
                return ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }

            // Check mod id
            if (modProfileDetails.modId == null)
            {
                Logger.Log(LogLevel.Error,
                    "The provided ModfileDetails has not been assigned a ModId. Ensure"
                    + " you assign the Id of the mod you intend to edit to the ModProfileDetails.modId"
                    + " field.");
                return ResultBuilder.Create(ResultCode.InvalidParameter_MissingModId);
            }

            // Check disableUploads
            if (Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error, "The current plugin configuration has uploading disabled.");
                return ResultBuilder.Create(ResultCode.Settings_UploadsDisabled);
            }

            var callbackConfirmation = openCallbacks.New();

            Result result;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result)
                                          && IsModProfileDetailsValidForEdit(modProfileDetails, out result))
            {
                // This will compress the images (if they exist) and add them to the request
                // TODO Add progress handle to the compress method
                var addModMediaResult = await AddModMedia.Request(modProfileDetails);
                result = addModMediaResult.result;

                if (result.Succeeded())
                {
                    WebRequestConfig config = addModMediaResult.value;
                    var task = WebRequestManager.Request<ModMediaObject>(config);
                    var resultAnd = await openCallbacks.Run(callbackConfirmation, task);
                    result = resultAnd.result;

                    if (!result.Succeeded() && currentUploadHandle != null)
                        currentUploadHandle.Failed = true;
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        public static Task<ResultAnd<ModProfile>> ReorderModMedia(ModId modId, string[] orderedFilenames, Action<ResultAnd<ModProfile>> callback = null)
            => EditModMedia(
                modId,
                API.Requests.ReorderModMedia.Request(modId, orderedFilenames),
                "You must provide a valid mod id to reorder media.",
                "You must provide all ordered filenames for the mod.",
                callback
            );

        public static Task<ResultAnd<ModProfile>> DeleteModMedia(ModId modId, string[] filenames, Action<ResultAnd<ModProfile>> callback = null) =>
            EditModMedia(
                modId,
                API.Requests.DeleteModMedia.Request(modId, filenames),
                "You must provide a valid mod id to delete media.",
                "You must provide filenames to be deleted from the mod.",
                callback
            );

        static async Task<ResultAnd<ModProfile>> EditModMedia(ModId modId, WebRequestConfig config, string invalidIdError, string invalidFilenamesError, Action<ResultAnd<ModProfile>> callback)
        {
            if (Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error, "The current plugin configuration has uploading disabled.");

                return ResultAnd.Create(ResultBuilder.Create(ResultCode.Settings_UploadsDisabled), (ModProfile)default);
            }

            Result result;

            if (modId == 0)
            {
                Logger.Log(LogLevel.Error, invalidIdError);
                result = ResultBuilder.Create(ResultCode.InvalidParameter_MissingModId);
            }
            else if (!config.HasStringData)
            {
                Logger.Log(LogLevel.Error, invalidFilenamesError);
                result = ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }
            else if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                TaskCompletionSource<bool> callbackConfirmation = openCallbacks.New();

                ResultAnd<MessageObject> taskResult = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<MessageObject>(config));

                openCallbacks.Complete(callbackConfirmation);

                result = taskResult.result;
            }

            ResultAnd<ModProfile> modProfile = result.Succeeded()
                ? await GetModSkipCache(modId)
                : ResultAnd.Create(result, (ModProfile)default);

            callback?.Invoke(modProfile);

            return modProfile;
        }

        private static SemaphoreSlim addModfileSemaphore = new SemaphoreSlim(1, 1);

        public static async Task<Result> AddModfile(ModfileDetails modfile)
        {
            // - Early outs -
            // Check Modfile
            if (modfile == null)
            {
                Logger.Log(LogLevel.Error, "ModfileDetails parameter cannot be null.");

                return ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }

            // Check mod id
            if (modfile.modId == null)
            {
                Logger.Log(
                    LogLevel.Error,
                    "The provided ModfileDetails has not been assigned a ModId. Ensure"
                    + " you assign the Id of the mod you intend to edit to the ModProfileDetails.modId"
                    + " field.");

                return ResultBuilder.Create(ResultCode.InvalidParameter_MissingModId);
            }

            // Check disableUploads
            if (Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error,
                    "The current plugin configuration has uploading disabled.");

                return ResultBuilder.Create(ResultCode.Settings_UploadsDisabled);
            }

            await addModfileSemaphore.WaitAsync();

            Result result;

            var callbackConfirmation = openCallbacks.New();
            try
            {

                ProgressHandle progressHandle = new ProgressHandle();
                currentUploadHandle = progressHandle;
                currentUploadHandle.OperationType = ModManagementOperationType.Upload;

                //------------------------------[ Setup callback param ]-------------------------------

                if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result)
                                              && IsModfileDetailsValid(modfile, out result))
                {
                    Stream stream = null;
                    if (modfile.directory != null)
                    {
                        var zipPath = DataStorage.GetUploadFilePath(modfile.modId.Value.id);
                        stream = DataStorage.temp.OpenWriteStream(zipPath, out result);
                        CompressOperationDirectory compressOperation = new CompressOperationDirectory(modfile.directory);

                        Task<Result> compressTask = compressOperation.Compress(stream);

                        Result compressionTaskResult = await openCallbacks.Run(callbackConfirmation, compressTask);
                        result = compressionTaskResult;

                        if (result.Succeeded())
                        {
                            Logger.Log(LogLevel.Verbose, $"Compressed file ({modfile.directory})"
                                                         + $"\nstream length: {stream?.Length}");
                        }
                        else
                        {
                            //      Compression FAILED
                            currentUploadHandle.Failed = true;
                            Logger.Log(LogLevel.Error, "Failed to compress the files at the "
                                                       + $"given directory ({modfile.directory}).");
                        }
                    }

                    if (result.Succeeded())
                    {
                        const long MIBS_100 = 104857600;
                        if (stream == null || stream.Length < MIBS_100)
                        {
                            callbackConfirmation = openCallbacks.New();
                            var requestConfig = await API.Requests.AddModFile.Request(modfile, stream);

                            Task<ResultAnd<ModfileObject>> task = WebRequestManager.Request<ModfileObject>(requestConfig, currentUploadHandle);
                            ResultAnd<ModfileObject> uploadResult = await openCallbacks.Run(callbackConfirmation, task);
                            result = uploadResult.result;

                            if (!result.Succeeded())
                            {
                                currentUploadHandle.Failed = true;
                            }
                            else
                            {
                                // TODO only remove the mod of the ID that we uploaded modfile.modId - add the modfile object we got back from the server to the cache
                                ResponseCache.ClearCache();

                                Logger.Log(LogLevel.Verbose, $"UPLOAD SUCCEEDED [{modfile.modId}_{uploadResult.value.id}]");
                            }
                        }
                        else
                        {
                            var nonce = $"{modfile.modId.Value.id}_{stream.Length}_{DateTime.UtcNow.Ticks}";

                            var response = await openCallbacks.Run(callbackConfirmation, CreateMultipartUploadSession((ModId)modfile.modId, "upload.zip", nonce));
                            result = response.result;
                            string uploadId = response.value.upload_id;

                            PaginatedResponse<MultipartUploadPart> partsObject = default;
                            if (result.Succeeded())
                            {
                                SearchFilter filter = new SearchFilter();
                                filter.SetPageIndex(0);
                                filter.SetPageSize(10000);
                                var r = await openCallbacks.Run(callbackConfirmation, GetMultipartUploadParts((ModId)modfile.modId, uploadId, filter));
                                partsObject = r.value;
                                result = r.result;
                            }

                            if (result.Succeeded())
                            {
                                int partOffset = 0;
                                if (partsObject?.data != null)
                                    partOffset = partsObject.data.Length;
                                result = await openCallbacks.Run(callbackConfirmation, AddAllMultipartUploadParts((ModId)modfile.modId, response.value.upload_id, stream, partOffset));
                            }

                            if (result.Succeeded())
                            {
                                result = await openCallbacks.Run(callbackConfirmation, CompleteMultipartUploadSession((ModId)modfile.modId, response.value.upload_id));
                            }

                            if (result.Succeeded())
                            {
                                var m = new ModfileDetails { modId = (ModId)modfile.modId, uploadId = response.value.upload_id };
                                var requestConfig = await API.Requests.AddModFile.Request(m, null);

                                Task<ResultAnd<ModfileObject>> task = WebRequestManager.Request<ModfileObject>(requestConfig, currentUploadHandle);
                                ResultAnd<ModfileObject> uploadResult = await openCallbacks.Run(callbackConfirmation, task);
                                result = uploadResult.result;
                            }
                        }
                    }
                    stream?.Close();
                }
            }
            catch (Exception ex)
            {
                result = ResultBuilder.Create(ResultCode.FILEUPLOAD_Error);
                addModfileSemaphore.Release();
                throw ex;
            }
            finally
            {
                addModfileSemaphore.Release();
            }

            if (currentUploadHandle != null)
            {
                currentUploadHandle.Completed = true;
                currentUploadHandle = null;
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        private static async Task<Result> AddAllMultipartUploadParts(ModId modId, string uploadId, Stream stream, int partOffset = 0)
        {
            Result result = ResultBuilder.Unknown;
            int chunkSize = 52428800; // 50MiB
            var endByte = chunkSize - 1; //last byte of a chunk
            var startByte = partOffset * chunkSize;

            if (stream.CanSeek)
                stream.Position = 0;

            byte[] buffer = new byte[chunkSize];
            while (stream.Read(buffer, 0, chunkSize) > 0)
            {
                byte[] data;

                if (endByte >= stream.Length)
                {
                    endByte = (int)stream.Length - 1; //adjust end byte to match the last byte of the zip file
                }

                //shrink byte array if last part
                if (endByte - startByte < chunkSize)
                {
                    data = new byte[endByte + 1 - startByte];
                    Array.Copy(buffer, data, endByte + 1 - startByte);
                }
                else
                {
                    data = buffer;
                }

                result = await AddMultipartUploadParts(modId, uploadId, $"bytes {startByte}-{endByte}/{stream.Length}", null, data);
                if (!result.Succeeded())
                    return result;

                startByte = endByte + 1;
                endByte = startByte + chunkSize - 1;
            }

            return result;
        }

        public static async void AddModfile(ModfileDetails modfile, Action<Result> callback)
        {
            // Check for callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the UploadModfile method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            Result result = await AddModfile(modfile);
            callback?.Invoke(result);
        }

        public static async void UploadModMedia(ModProfileDetails modProfileDetails, Action<Result> callback)
        {
            // Check for callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the UploadModMedia method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            Result result = await UploadModMedia(modProfileDetails);
            callback?.Invoke(result);
        }

        public static async Task<Result> ArchiveModProfile(ModId modId)
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.DeleteMod.Request(modId);
                result = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request(config));
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        public static async void ArchiveModProfile(ModId modId, Action<Result> callback)
        {
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the ArchiveModProfile method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            Result result = await ArchiveModProfile(modId);
            callback?.Invoke(result);
        }

        static bool IsModfileDetailsValid(ModfileDetails modfile, out Result result)
        {
            // Check directory exists
            if (modfile.uploadId == null && !DataStorage.TryGetModfileDetailsDirectory(modfile.directory,
                    out string _))
            {
                Logger.Log(LogLevel.Error,
                    "The provided directory in ModfileDetails could not be found or"
                    + $" does not exist ({modfile.directory}).");
                result = ResultBuilder.Create(ResultCode.IO_DirectoryDoesNotExist);
                return false;
            }

            // check metadata isn't too large
            if (modfile.metadata?.Length > 50000)
            {
                Logger.Log(LogLevel.Error,
                    "The provided metadata in ModProfileDetails exceeds 50,000 characters"
                    + $"\n(Was given {modfile.metadata.Length} characters)");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_ModMetadataTooLarge);
                return false;
            }

            // check changelog isn't too large
            if (modfile.changelog?.Length > 50000)
            {
                Logger.Log(LogLevel.Error,
                    "The provided changelog in ModProfileDetails exceeds 50,000 characters"
                    + $"(Was given {modfile.changelog})");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_ChangeLogTooLarge);
                return false;
            }

            result = ResultBuilder.Success;
            return true;
        }

        static bool IsModProfileDetailsValid(ModProfileDetails modDetails, out Result result)
        {
            if (modDetails.logo == null || string.IsNullOrWhiteSpace(modDetails.summary)
                                        || string.IsNullOrWhiteSpace(modDetails.name))
            {
                Logger.Log(
                    LogLevel.Error,
                    "The required fields in ModProfileDetails have not been set."
                    + " Make sure the Name, Logo and Summary have been assigned before attempting"
                    + "to submit a new Mod Profile");
                result = ResultBuilder.Create(
                    (ResultCode.InvalidParameter_ModProfileRequiredFieldsNotSet));
                return false;
            }

            return IsModProfileDetailsValidForEdit(modDetails, out result);
        }

        static bool IsModProfileDetailsValidForEdit(ModProfileDetails modDetails, out Result result)
        {
            if (modDetails.summary?.Length > 250)
            {
                Logger.Log(LogLevel.Error,
                    "The provided summary in ModProfileDetails exceeds 250 characters");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_ModSummaryTooLarge);
                return false;
            }

            if (modDetails.logo != null)
            {
                if (modDetails.logo.EncodeToPNG().Length > 8388608)
                {
                    Logger.Log(LogLevel.Error,
                        "The provided logo in ModProfileDetails exceeds 8 megabytes");
                    result = ResultBuilder.Create(ResultCode.InvalidParameter_ModLogoTooLarge);
                    return false;
                }
            }

            if (modDetails.metadata?.Length > 50000)
            {
                Logger.Log(LogLevel.Error,
                    "The provided metadata in ModProfileDetails exceeds 50,000 characters"
                    + $"(Was given {modDetails.metadata.Length})");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_ModMetadataTooLarge);
                return false;
            }

            if (modDetails.description?.Length > 50000)
            {
                Logger.Log(LogLevel.Error,
                    "The provided description in ModProfileDetails exceeds 50,000 characters"
                    + $"(Was given {modDetails.description.Length})");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_DescriptionTooLarge);

                return false;
            }

            result = ResultBuilder.Success;
            return true;
        }

        public static async Task<ResultAnd<ModPage>> GetCurrentUserCreations(SearchFilter filter)
        {
            var callbackConfirmation = openCallbacks.New();

            Result result;
            ModPage page = new ModPage();

            var config = API.Requests.GetCurrentUserCreations.Request(filter);

            int offset = filter.pageIndex * filter.pageSize;
            if (IsInitialized(out result) && IsSearchFilterValid(filter, out result)
                                          && IsAuthenticatedSessionValid(out result)
                                          && !ResponseCache.GetModsFromCache(config.Url, offset, filter.pageSize, out page))
            {

                var task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<API.Requests.GetCurrentUserCreations.ResponseSchema>(config));

                result = task.result;

                if (result.Succeeded())
                {
                    page = ResponseTranslator.ConvertResponseSchemaToModPage(task.value, filter);

                    ResponseCache.AddModsToCache(config.Url, offset, page);

                    if (page.modProfiles.Length > filter.pageSize)
                    {
                        Array.Copy(page.modProfiles, page.modProfiles, filter.pageSize);
                    }
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(result, page);
        }

        public static async void GetCurrentUserCreations(SearchFilter filter, Action<ResultAnd<ModPage>> callback)
        {
            // Check for callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetCurrentUserCreations method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            ResultAnd<ModPage> result = await GetCurrentUserCreations(filter);
            callback?.Invoke(result);
        }

        #endregion // Mod Uploading

        #region Multipart

        public static async Task<ResultAnd<PaginatedResponse<MultipartUploadPart>>> GetMultipartUploadParts(ModId modId, string uploadId, SearchFilter filter)
        {
            var callbackConfirmation = openCallbacks.New();
            ResultAnd<PaginatedResponse<MultipartUploadPart>> response = ResultAnd.Create(ResultCode.Unknown, new PaginatedResponse<MultipartUploadPart>());

            if (IsInitialized(out response.result) && IsAuthenticatedSessionValid(out response.result))
            {
                var config = API.Requests.GetMultipartUploadParts.Request(modId, uploadId, filter);
                response = await openCallbacks.Run(callbackConfirmation,
                    WebRequestManager.Request<PaginatedResponse<MultipartUploadPart>>(config));
            }

            openCallbacks.Complete(callbackConfirmation);
            return response;
        }

        public static async void GetMultipartUploadParts(ModId modId, string uploadId, SearchFilter filter, Action<ResultAnd<PaginatedResponse<MultipartUploadPart>>> callback)
        {
            // Callback warning
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetMultipartUploadParts method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            ResultAnd<PaginatedResponse<MultipartUploadPart>> result = await GetMultipartUploadParts(modId, uploadId, filter);
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<PaginatedResponse<MultipartUpload>>> GetMultipartUploadSessions(ModId modId, SearchFilter filter)
        {
            var callbackConfirmation = openCallbacks.New();
            ResultAnd<PaginatedResponse<MultipartUpload>> resultAnd = ResultAnd.Create(ResultCode.Unknown, new PaginatedResponse<MultipartUpload>());

            if (IsInitialized(out resultAnd.result) && IsAuthenticatedSessionValid(out resultAnd.result))
            {
                var config = API.Requests.GetMultipartUploadSession.Request(modId, filter);
                var response = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<PaginatedResponse<MultipartUpload>>(config));
                resultAnd = response;
            }

            openCallbacks.Complete(callbackConfirmation);
            return resultAnd;
        }

        public static async void GetMultipartUploadSessions(ModId modId, SearchFilter filter, Action<ResultAnd<PaginatedResponse<MultipartUpload>>> callback)
        {
            // Callback warning
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetMultipartSessions method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            ResultAnd<PaginatedResponse<MultipartUpload>> result = await GetMultipartUploadSessions(modId, filter);
            callback?.Invoke(result);
        }

        public static async Task<Result> AddMultipartUploadParts(ModId modId, string uploadId, string contentRange, string digest, byte[] rawBytes)
        {
            var callbackConfirmation = openCallbacks.New();

            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.AddMultipartUploadParts.Request(modId, uploadId, contentRange, digest, rawBytes);
                var response = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<MultipartUpload>(config));
                result = response.result;
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        public static async void AddMultipartUploadParts(ModId modId, string uploadId, string contentRange, string digest, byte[] rawBytes, Action<Result> callback)
        {
            // Callback warning
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the AddMultipartUploadParts method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await AddMultipartUploadParts(modId, uploadId, contentRange, digest, rawBytes);
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<MultipartUpload>> CreateMultipartUploadSession(ModId modId, string filename, string nonce = null)
        {
            var callbackConfirmation = openCallbacks.New();
            var resultAnd = ResultAnd.Create(ResultCode.Unknown, new MultipartUpload());
            if (IsInitialized(out resultAnd.result) && IsAuthenticatedSessionValid(out resultAnd.result))
            {
                var config = API.Requests.CreateMultipartUploadSession.Request(modId, filename, nonce);
                resultAnd = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<MultipartUpload>(config));
            }

            openCallbacks.Complete(callbackConfirmation);
            return resultAnd;
        }

        public static async void CreateMultipartUploadSession(ModId modId, string filename, string nonce, Action<ResultAnd<MultipartUpload>> callback)
        {
            // Callback warning
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the CreateMultipartUploadSession method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            ResultAnd<MultipartUpload> result = await CreateMultipartUploadSession(modId, filename, nonce);
            callback?.Invoke(result);
        }

        public static async Task<Result> DeleteMultipartUploadSession(ModId modId, string uploadId)
        {
            var callbackConfirmation = openCallbacks.New();

            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.DeleteMultipartUploadSession.Request(modId, uploadId);
                var response = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request(config));
                result = response;
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        public static async void DeleteMultipartUploadSession(ModId modId, string uploadId, Action<Result> callback)
        {
            // Callback warning
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the DeleteMultipartUploadSession method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await DeleteMultipartUploadSession(modId, uploadId);
            callback?.Invoke(result);
        }

        public static async Task<Result> CompleteMultipartUploadSession(ModId modId, string uploadId)
        {
            var callbackConfirmation = openCallbacks.New();

            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.CompleteMultipartUploadSession.Request(modId, uploadId);
                var response = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request(config));
                result = response;
            }

            openCallbacks.Complete(callbackConfirmation);
            return result;
        }

        public static async void CompleteMultipartUploadSession(ModId modId, string uploadId, Action<Result> callback)
        {
            // Callback warning
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the CompleteMultipartUploadSession method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await CompleteMultipartUploadSession(modId, uploadId);
            callback?.Invoke(result);
        }

        #endregion

        #region Monetization

        public static async Task<ResultAnd<TokenPack[]>> GetTokenPacks(Action<ResultAnd<TokenPack[]>> callback = null)
        {
            var callbackConfirmation = openCallbacks.New();

            TokenPack[] tokenPacks = Array.Empty<TokenPack>();

            if (IsInitialized(out Result result) && !ResponseCache.GetTokenPacksFromCache(out tokenPacks))
            {
                var config = API.Requests.GetTokenPacks.Request();
                var task = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<GetTokenPacks.ResponseSchema>(config));

                result = task.result;
                if (result.Succeeded())
                {
                    tokenPacks = ResponseTranslator.ConvertTokenPackObjectsToTokenPacks(task.value.data);
                    ResponseCache.AddTokenPacksToCache(tokenPacks);
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            var resultAnd = ResultAnd.Create(result, tokenPacks);

            callback?.Invoke(resultAnd);

            return resultAnd;
        }

        public static async Task<ResultAnd<Entitlement[]>> SyncEntitlements()
        {
            Task<ResultAnd<SyncEntitlements.ResponseSchema>> requestTask = null;
            Entitlement[] entitlements = null;
            WebRequestConfig config = null;

#if UNITY_GAMECORE && !UNITY_EDITOR
            var token = await GamecoreHelper.GetToken();
            config = API.Requests.SyncEntitlements.XboxRequest(token);
            requestTask = WebRequestManager.Request<SyncEntitlements.ResponseSchema>(config);
#elif UNITY_PS4 && !UNITY_EDITOR
            var token = await PS4Helper.GetAuthCodeForEntitlements();
            if(Settings.build is PlaystationBuildSettings psb)
            {
                config = API.Requests.SyncEntitlements.PsnRequest(token.code, token.environment, psb.serviceLabel);
                requestTask = WebRequestManager.Request<SyncEntitlements.ResponseSchema>(config);
            }
#elif UNITY_PS5 && !UNITY_EDITOR
            var token = await PS5Helper.GetAuthCodeForEntitlements();
            if(Settings.build is PlaystationBuildSettings psb)
            {
                config = API.Requests.SyncEntitlements.PsnRequest(token.code, token.environment, psb.serviceLabel);
                requestTask = WebRequestManager.Request<SyncEntitlements.ResponseSchema>(config);
            }
#elif UNITY_STANDALONE
            config = API.Requests.SyncEntitlements.SteamRequest();
            requestTask = WebRequestManager.Request<SyncEntitlements.ResponseSchema>(config);
#else
            return ResultAnd.Create<Entitlement[]>(ResultBuilder.Create(ResultCode.User_NotAuthenticated), null);
#endif

            var callbackConfirmation = openCallbacks.New();

            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                result = await IsMarketplaceEnabled();
                if (result.Succeeded())
                {
                    try
                    {
                        var task = await openCallbacks.Run(callbackConfirmation, requestTask);

                        result = task.result;

                        if (result.Succeeded())
                        {
                            entitlements = ResponseTranslator.ConvertEntitlementObjectsToEntitlements(task.value.data);
                            ResponseCache.ReplaceEntitlements(entitlements);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogLevel.Error, $"FAILED: {e.Message}");
                        return ResultAnd.Create<Entitlement[]>(result, null);
                    }
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks_dictionary.Remove(callbackConfirmation);

            return ResultAnd.Create(result, entitlements);
        }

        public static async void SyncEntitlements(Action<ResultAnd<Entitlement[]>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the SyncSteamEntitlements method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }

            var result = await SyncEntitlements();
            callback(result);
        }

        public static async Task<ResultAnd<CheckoutProcess>> PurchaseMod(ModId modId, int displayAmount, string idempotent)
        {
            if (Regex.IsMatch(idempotent, "^[a-zA-Z0-9-]+$"))
            {
                Logger.Log(LogLevel.Warning, "ModIOUnityImplementation.PurchaseMod: Idempotent should be alphanumeric, should not contain unique characters except for -.");
            }

            var callbackConfirmation = openCallbacks.New();

            ResultAnd<CheckoutProcess> checkoutProcess = ResultAnd.Create<CheckoutProcess>(ResultCode.Unknown, default);

            if (IsInitialized(out checkoutProcess.result) && IsAuthenticatedSessionValid(out checkoutProcess.result))
            {
                checkoutProcess.result = await IsMarketplaceEnabled();
                if (checkoutProcess.result.Succeeded())
                {
                    var config = API.Requests.PurchaseMod.Request(modId, displayAmount, idempotent);
                    var resultAnd = await openCallbacks.Run(callbackConfirmation, WebRequestManager.Request<CheckoutProcessObject>(config));

                    if (resultAnd.result.Succeeded())
                    {
                        checkoutProcess = ResultAnd.Create(resultAnd.result, ResponseTranslator.ConvertCheckoutProcessObjectToCheckoutProcess(resultAnd.value));
                        ResponseCache.UpdateWallet(resultAnd.value.balance);
                        ModCollectionManager.UpdateModCollectionEntry(modId, resultAnd.value.mod);
                        ModCollectionManager.AddModToUserPurchases(modId);
                        ModManagement.WakeUp();
                    }
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return checkoutProcess;
        }

        public static async void PurchaseMod(ModId modId, int displayAmount, string idempotent, Action<ResultAnd<CheckoutProcess>> callback)
        {
            // Check for callback
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the CompleteAMarketplacePurchase method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            ResultAnd<CheckoutProcess> result = await PurchaseMod(modId, displayAmount, idempotent);
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<ModPage>> GetUserPurchases(SearchFilter filter)
        {
            TaskCompletionSource<bool> callbackConfirmation = openCallbacks.New();

            ModPage page = new ModPage();

            string unpaginatedURL = API.Requests.GetUserPurchases.UnpaginatedURL(filter);
            var offset = filter.pageIndex * filter.pageSize;

            if (IsInitialized(out Result result) && IsSearchFilterValid(filter, out result)
                                                 && !ResponseCache.GetModsFromCache(unpaginatedURL, offset, filter.pageSize, out page))
            {
                var config = API.Requests.GetUserPurchases.Request(filter);

                var task = await openCallbacks.Run(callbackConfirmation,
                    WebRequestManager.Request<API.Requests.GetUserPurchases.ResponseSchema>(config));

                result = task.result;

                if (result.Succeeded())
                {
                    page = ResponseTranslator.ConvertResponseSchemaToModPage(task.value, filter);

                    // Return the exact number of mods that were requested (not more)
                    if (page.modProfiles.Length > filter.pageSize)
                    {
                        Array.Copy(page.modProfiles, page.modProfiles, filter.pageSize);
                    }
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(result, page);
        }

        public static async void GetUserPurchases(SearchFilter filter, Action<ResultAnd<ModPage>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetUserPurchases method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            ResultAnd<ModPage> result = await GetUserPurchases(filter);
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<Wallet>> GetUserWalletBalance()
        {
            var callbackConfirmation = openCallbacks.New();

            ResultAnd<WalletObject> resultAnd = ResultAnd.Create<WalletObject>(ResultCode.Success, default);
            Wallet wallet = default;
            if (IsInitialized(out resultAnd.result) && IsAuthenticatedSessionValid(out resultAnd.result)
                                                    && !ResponseCache.GetWalletFromCache(out wallet))
            {
                resultAnd.result = await IsMarketplaceEnabled();
                if (resultAnd.result.Succeeded())
                {
                    var config = API.Requests.GetUserWalletBalance.Request();
                    resultAnd = await openCallbacks.Run(callbackConfirmation,
                        WebRequestManager.Request<WalletObject>(config));
                    if (resultAnd.result.Succeeded())
                    {
                        wallet = ResponseTranslator.ConvertWalletObjectToWallet(resultAnd.value);
                        ResponseCache.UpdateWallet(resultAnd.value);
                    }
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(resultAnd.result, wallet);
        }

        public static async void GetUserWalletBalance(Action<ResultAnd<Wallet>> callback)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetUserPurchases method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            ResultAnd<Wallet> result = await GetUserWalletBalance();
            callback?.Invoke(result);
        }

        public static async Task<ResultAnd<MonetizationTeamAccount[]>> GetModMonetizationTeam(ModId modId)
        {
            var callbackConfirmation = openCallbacks.New();


            Result result;
            MonetizationTeamAccount[] teamAccounts = default;

            if (IsInitialized(out result) && IsAuthenticatedSessionValid(out result)
                                          && !ResponseCache.GetModMonetizationTeamCache(modId, out teamAccounts))
            {
                var task = await API.Requests.GetModMonetizationTeam.Request(modId).RunViaWebRequestManager();

                result = task.result;
                if (task.result.Succeeded())
                {
                    teamAccounts = ResponseTranslator.ConvertGameMonetizationTeamObjectsToGameMonetizationTeams(task.value.data);

                    ResponseCache.AddModMonetizationTeamToCache(modId, teamAccounts);
                }
            }

            openCallbacks.Complete(callbackConfirmation);

            return ResultAnd.Create(result, teamAccounts);
        }

        public static async void GetModMonetizationTeam(Action<ResultAnd<MonetizationTeamAccount[]>> callback, ModId modId)
        {
            // Early out
            if (callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetModMonetizationTeam method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            ResultAnd<MonetizationTeamAccount[]> result = await GetModMonetizationTeam(modId);
            callback?.Invoke(result);
        }

        public static async Task<Result> AddModMonetizationTeam(ModId modId, ICollection<ModMonetizationTeamDetails> team)
        {
            var callbackConfirmation = openCallbacks.New();

            if (IsInitialized(out Result result) && IsAuthenticatedSessionValid(out result))
            {
                var config = API.Requests.AddModMonetizationTeam.Request(modId, team);
                result = await openCallbacks.Run(callbackConfirmation,
                    WebRequestManager.Request(config));

                ResponseCache.ClearModMonetizationTeamFromCache(modId);
            }

            openCallbacks.Complete(callbackConfirmation);

            return result;
        }

        public static async void AddModMonetizationTeam(Action<Result> callback, ModId modId, ICollection<ModMonetizationTeamDetails> team)
        {
            Result result = await AddModMonetizationTeam(modId, team);
            callback?.Invoke(result);
        }

        #endregion //Monetization
    }
}
