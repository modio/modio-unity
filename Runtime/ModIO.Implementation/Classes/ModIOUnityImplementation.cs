using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Threading.Tasks;
using JetBrains.Annotations;
using ModIO.Implementation.API;
using ModIO.Implementation.API.Requests;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.Platform;
using UnityEngine;
using System.Linq;

namespace ModIO.Implementation
{
    /// <summary>
    /// The actual implementation for methods called from the ModIOUnity interface
    /// </summary>
    internal static class ModIOUnityImplementation
    {
        // REVIEW @Jackson Not sure if this should go into mod management or just here. It doesnt
        // really belong anywhere in particular.
        /// <summary>
        /// A cached reference to the current upload operation handle.
        /// </summary>
        static ProgressHandle currentUploadHandle;

        /// <summary>
        /// Everytime an implemented method with a callback is used it creates a
        /// TaskCompletionSource and adds it to this hashset. Shutdown will make sure to wait for
        /// all of these callbacks to return before invoking the final shutdown callback.
        /// </summary>
        static Dictionary<TaskCompletionSource<bool>, Task> openCallbacks =
            new Dictionary<TaskCompletionSource<bool>, Task>();

        /// <summary>
        /// cached Task of the shutdown operation so we dont run several shutdowns simultaneously
        /// </summary>
        static Task shutdownOperation;

#region Synchronous Requirement Checks - to detect early outs and failures

        /// <summary>Has the plugin been initialized.</summary>
        internal static bool isInitialized;

        /// <summary>
        /// Flagged to true if the plugin is being shutdown
        /// </summary>
        public static bool shuttingDown;

        /// <summary>Has the plugin been initialized.</summary>
        public static bool IsInitialized(out Result result)
        {
            if(ModIOUnityImplementation.isInitialized)
            {
                result = ResultBuilder.Success;
                return true;
            }
            else
            {
                result = ResultBuilder.Create(ResultCode.Init_NotYetInitialized);
                Logger.Log(
                    LogLevel.Error,
                    "You attempted to use a method but the plugin hasn't been initialized yet."
                    + " Be sure to use ModIOUnity.InitializeForUser to initialize the plugin "
                    + "before attempting this method again (Or ModIOUnityAsync.InitializeForUser).");
                return false;
            }
        }

        /// <summary>Checks the state of the credentials used to authenticate.</summary>
        public static bool AreCredentialsValid(bool requireUserToken, out Result result)
        {
            if(!requireUserToken && string.IsNullOrEmpty(UserData.instance.oAuthToken))
            {
                // TODO(@jackson): Check the Game Key state here
            }
            else if(UserData.instance != null) // Token required or exists
            {
                if(string.IsNullOrEmpty(UserData.instance.oAuthToken))
                {
                    result = ResultBuilder.Create(ResultCode.User_NotAuthenticated);
                    return false;
                }
                else if(UserData.instance.oAuthTokenWasRejected)
                {
                    result = ResultBuilder.Create(ResultCode.User_InvalidToken);
                    return false;
                }
                // TODO(@jackson): Check expiry
            }
            else
            {
                result = ResultBuilder.Create(ResultCode.Init_NotYetInitialized);
                return false;
            }

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
            if(filter == null)
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
        public static async Task<Result> InitializeForUserAsync(string userProfileIdentifier,
                                                                ServerSettings serverSettings,
                                                                BuildSettings buildSettings)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            // clean user profile identifier in case of filename usage
            userProfileIdentifier = IOUtil.CleanFileNameForInvalidCharacters(userProfileIdentifier);
            
            Settings.server = serverSettings;
            Settings.build = buildSettings;

            Result result = ResultBuilder.Success;

            // - load data services -
            // NOTE(@jackson):
            //  The order of the data module loading is important on standalone platforms.
            //  The UserDataService must be loaded before the PersistentDataService to ensure we
            //  load a potential persistent directory override stored in the user's json file. A
            //  directory override will be loaded in to the BuildSettings.extData field.

            // TODO(@jackson): Handle errors
            Task<ResultAnd<IUserDataService>> userDataTask =
                PlatformConfiguration.CreateUserDataService(userProfileIdentifier,
                    serverSettings.gameId, buildSettings);
            openCallbacks[callbackConfirmation] = userDataTask;
            ResultAnd<IUserDataService> createUDS = await userDataTask;
            openCallbacks[callbackConfirmation] = null;
            DataStorage.user = createUDS.value;

            Task<ResultAnd<IPersistentDataService>> persistentDataTask =
                PlatformConfiguration.CreatePersistentDataService(serverSettings.gameId,
                    buildSettings);
            openCallbacks[callbackConfirmation] = persistentDataTask;
            ResultAnd<IPersistentDataService> createPDS = await persistentDataTask;
            openCallbacks[callbackConfirmation] = null;

            DataStorage.persistent = createPDS.value;

            Task<ResultAnd<ITempDataService>> tempDataTask =
                PlatformConfiguration.CreateTempDataService(serverSettings.gameId, buildSettings);
            openCallbacks[callbackConfirmation] = tempDataTask;
            ResultAnd<ITempDataService> createTDS = await tempDataTask;
            openCallbacks[callbackConfirmation] = null;
            DataStorage.temp = createTDS.value;

            // - load user data -
            Task<Result> dataStorageTask = DataStorage.LoadUserData();

            openCallbacks[callbackConfirmation] = dataStorageTask;
            result = await dataStorageTask;
            openCallbacks[callbackConfirmation] = null;

            if(result.code == ResultCode.IO_FileDoesNotExist
               || result.code == ResultCode.IO_DirectoryDoesNotExist)
            {
                UserData.instance = new UserData();
                result = await DataStorage.SaveUserData();
            }

            // TODO We need to have one line that invokes

            if(!result.Succeeded())
            {
                // TODO(@jackson): Prepare for public
                callbackConfirmation.SetResult(true);
                openCallbacks.Remove(callbackConfirmation);
                return result;
            }

            // - load registry -
            Task<Result> registryTask = ModCollectionManager.LoadRegistry();

            Logger.Log(LogLevel.Verbose, "LOADING REGISTRY");
            openCallbacks[callbackConfirmation] = registryTask;
            result = await registryTask;
            Logger.Log(LogLevel.Verbose, "loaded REGISTRY");
            openCallbacks[callbackConfirmation] = null;

            if(!result.Succeeded())
            {
                callbackConfirmation.SetResult(true);
                openCallbacks.Remove(callbackConfirmation);
                return result;
            }

            // - finalize -
            ModIOUnityImplementation.isInitialized = true;

            result = ResultBuilder.Success;
            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            Logger.Log(LogLevel.Message, $"Initialized User[{userProfileIdentifier}]");

            return result;
        }

        /// <summary>Initializes the Plugin for the given settings. Loads the
        /// state of mods installed on the system as well as the set of mods the
        /// specified user has installed on this device.</summary>
        public static async void InitializeForUserAsync(string userProfileIdentifier,
                                                        ServerSettings serverSettings,
                                                        BuildSettings buildSettings,
                                                        Action<Result> callback)
        {
            Result result = await InitializeForUserAsync(userProfileIdentifier,
                serverSettings, buildSettings);

            callback(result);
        }

        /// <summary>Initializes the Plugin for the given settings. Loads the
        /// state of mods installed on the system as well as the set of mods the
        /// specified user has installed on this device.</summary>
        public static async Task<Result> InitializeForUserAsync(string userProfileIdentifier)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            ServerSettings serverSettings;
            BuildSettings buildSettings;

            Result r = SettingsAsset.TryLoad(out serverSettings, out buildSettings);

            if(r.Succeeded())
            {
                Task<Result> initTask = ModIOUnityImplementation.InitializeForUserAsync(
                    userProfileIdentifier, serverSettings, buildSettings);

                openCallbacks[callbackConfirmation] = initTask;
                r = await initTask;
                openCallbacks[callbackConfirmation] = null;
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);
            return r;
        }

        /// <summary>Initializes the Plugin for the given settings. Loads the
        /// state of mods installed on the system as well as the set of mods the
        /// specified user has installed on this device.</summary>
        public static async void InitializeForUserAsync(string userProfileIdentifier,
                                                        Action<Result> callback)
        {
            var result = await InitializeForUserAsync(userProfileIdentifier);
            callback(result);
        }

        /// <summary>
        /// Cancels any running public operations, frees plugin resources, and invokes
        /// any pending callbacks with a cancelled result code.
        /// </summary>
        public static async Task Shutdown(Action shutdownComplete)
        {
            if(!IsInitialized(out Result _))
            {
                Logger.Log(LogLevel.Verbose, "ALREADY SHUTDOWN");
                return;
            }
            
            // This first block ensures we dont have conflicting shutdown operations
            // being called at the same time.
            if(shuttingDown && shutdownOperation != null)
            {
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

                    shutdownOperation = null;
                    
                    shuttingDown = false;
                }
                catch(Exception e)
                {
                    shuttingDown = false;
                    Logger.Log(LogLevel.Error, $"Exception caught when shutting down plugin: {e.Message} - inner={e.InnerException?.Message}");
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
            RESTAPI.Shutdown(); 
            await ModManagement.ShutdownOperations(); 

            isInitialized = false;
            UserData.instance = null;
            // Settings.server = default;
            // Settings.build = default;
            ResponseCache.ClearCache();
            ModCollectionManager.ClearRegistry();

            // get new instance of dictionary so it's thread safe
            Dictionary<TaskCompletionSource<bool>, Task> tasks =
                new Dictionary<TaskCompletionSource<bool>, Task>(openCallbacks);

            // iterate over the tasks and await for non faulted callbacks to finish
            using(var enumerator = tasks.GetEnumerator())
            {
                while(enumerator.MoveNext())
                {
                    if(enumerator.Current.Value != null && enumerator.Current.Value.IsFaulted)
                    {
                        Logger.Log(LogLevel.Error,
                            "An Unhandled Exception was thrown in"
                            + " an awaited task. The corresponding callback"
                            + " will never be invoked.");
                        if(openCallbacks.ContainsKey(enumerator.Current.Key))
                        {
                            openCallbacks.Remove(enumerator.Current.Key);
                        }
                    }
                    else
                    {
                        await enumerator.Current.Key.Task;
                    }
                }
            }
        }

#endregion // Initialization and Maintenance

#region Authentication

        public static async Task<Result> IsAuthenticated()
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            Result result = ResultBuilder.Unknown;

            if(IsInitialized(out result) && AreCredentialsValid(true, out result))
            {
                string url = GetAuthenticatedUser.URL();

                Task<ResultAnd<UserObject>> task =
                    RESTAPI.Request<UserObject>(url, GetAuthenticatedUser.Template);

                // We always cache the task while awaiting so we can check IsFaulted externally
                openCallbacks[callbackConfirmation] = task;
                ResultAnd<UserObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                if(response.result.Succeeded())
                {
                    result = response.result;
                    await UserData.instance.SetUserObject(response.value);
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);
            return result;
        }

        public static async void IsAuthenticated(Action<Result> callback)
        {
            if(callback == null)
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
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            if(IsInitialized(out result) && IsValidEmail(emailaddress, out result))
            {
                //      Synchronous checks SUCCEEDED


                string url = AuthenticateViaEmail.URL(emailaddress, out WWWForm form);

                Task<ResultAnd<AuthenticateViaEmail.ResponseSchema>> task =
                    RESTAPI.Request<AuthenticateViaEmail.ResponseSchema>(
                        url, AuthenticateViaEmail.Template, form);

                // We always cache the task while awaiting so we can check IsFaulted externally
                openCallbacks[callbackConfirmation] = task;
                ResultAnd<AuthenticateViaEmail.ResponseSchema> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(response.result.Succeeded())
                {
                    // Server request SUCCEEDED

                    result = ResultBuilder.Success;

                    // continue to invoke at the end of this method
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }


        public static async void RequestEmailAuthToken(string emailaddress, Action<Result> callback)
        {
            if(callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the RequestEmailAuthToken method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            var result = await RequestEmailAuthToken(emailaddress);
            callback(result);
        }

        public static async Task<Result> SubmitEmailSecurityCode(string securityCode)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result = ResultBuilder.Unknown;
            //-------------------------------------------------------------------------------------
            if(string.IsNullOrWhiteSpace(securityCode))
            {
                Logger.Log(
                    LogLevel.Warning,
                    "The security code provided is null. Be sure to use the 5 digit code"
                    + " sent to the specified email address when using RequestEmailAuthToken()");
                ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }
            else if(IsInitialized(out result))
            {
                //      Synchronous checks SUCCEEDED

                // Create Form fields
                string url =
                    API.Requests.AuthenticateUser.InternalURL(securityCode, out WWWForm form);

                Task<ResultAnd<AccessTokenObject>> task = RESTAPI.Request<AccessTokenObject>(
                    url, API.Requests.AuthenticateUser.Template, form);

                // We always cache the task while awaiting so we can check IsFaulted externally
                openCallbacks[callbackConfirmation] = task;
                ResultAnd<AccessTokenObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(response.result.Succeeded())
                {
                    // Server request SUCCEEDED

                    // Assign deserialized response as the token

                    // Set User Access Token
                    await UserData.instance.SetOAuthToken(response.value);

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
                    await GetCurrentUser(delegate { });

                    // continue to invoke at the end of this method
                }
            }

            //callback?.Invoke(result);
            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }

        public static async void SubmitEmailSecurityCode(string securityCode,
                                                         Action<Result> callback)
        {
            if(callback == null)
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
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            TermsOfUse termsOfUse = default;
            //-------------------------------------------------------------------------------------

            string url = GetTerms.URL();

            if(IsInitialized(out result) && !ResponseCache.GetTermsFromCache(url, out termsOfUse))
            {
                //      Synchronous checks SUCCEEDED

                Task<ResultAnd<TermsObject>> task =
                    RESTAPI.Request<TermsObject>(url, GetTerms.Template);

                // We always cache the task while awaiting so we can check IsFaulted externally
                openCallbacks[callbackConfirmation] = task;
                ResultAnd<TermsObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(response.result.Succeeded())
                {
                    // Server request SUCCEEDED

                    // convert response to user friendly TermsOfUse struct
                    termsOfUse = ResponseTranslator.ConvertTermsObjectToTermsOfUse(response.value);

                    // Add terms to cache
                    ResponseCache.AddTermsToCache(url, termsOfUse);
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return ResultAnd.Create(result, termsOfUse);
        }

        public static async void GetTermsOfUse(Action<ResultAnd<TermsOfUse>> callback)
        {
            if(callback == null)
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
            [CanBeNull] string emailAddress, [CanBeNull] TermsHash? hash, [CanBeNull] string nonce,
            [CanBeNull] OculusDevice? device, [CanBeNull] string userId)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            if(IsInitialized(out result)
               && (emailAddress == null || IsValidEmail(emailAddress, out result)))
            {
                //      Synchronous checks SUCCEEDED

                string url = API.Requests.AuthenticateUser.ExternalURL(
                    serviceProvider, data, hash, emailAddress,
                    // Oculus nonce, device, user_id
                    nonce, device, userId,
                    // -----------------------------
                    out WWWForm form);

                Task<ResultAnd<AccessTokenObject>> task = RESTAPI.Request<AccessTokenObject>(
                    url, API.Requests.AuthenticateUser.Template, form);

                // We always cache the task while awaiting so we can check IsFaulted externally
                openCallbacks[callbackConfirmation] = task;
                ResultAnd<AccessTokenObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(response.result.Succeeded())
                {
                    // Server request SUCCEEDED

                    // Set User Access Token
                    await UserData.instance.SetOAuthToken(response.value);

                    // TODO @Steve (see other example, same situation in email auth)
                    await GetCurrentUser(delegate { });
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }

        public static async void AuthenticateUser(
            string data, AuthenticationServiceProvider serviceProvider,
            [CanBeNull] string emailAddress, [CanBeNull] TermsHash? hash, [CanBeNull] string nonce,
            [CanBeNull] OculusDevice? device, [CanBeNull] string userId, Action<Result> callback)
        {
            if(callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the AuthenticateUser method. It is "
                    + "possible that this operation will not resolve successfully and should be "
                    + "checked with a proper callback.");
            }

            Result result = await AuthenticateUser(data, serviceProvider, emailAddress, hash, nonce, device, userId);
            callback?.Invoke(result);
        }


#endregion // Authentication

#region Mod Browsing

        
        public static async Task<ResultAnd<TagCategory[]>> GetGameTags()
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback params ]------------------------------
            Result result;
            TagCategory[] tags = new TagCategory[0];
            //-------------------------------------------------------------------------------------

            if(IsInitialized(out result) && !ResponseCache.GetTagsFromCache(out tags))
            {
                string url = API.Requests.GetGameTags.URL();

                // MAKE RESTAPI REQUEST
                Task<ResultAnd<GetGameTags.ResponseSchema>> task =
                    RESTAPI.Request<GetGameTags.ResponseSchema>(url,
                        API.Requests.GetGameTags.Template);

                // We always cache the task while awaiting so we can check IsFaulted externally
                openCallbacks[callbackConfirmation] = task;
                ResultAnd<GetGameTags.ResponseSchema> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(response.result.Succeeded())
                {
                    tags = ResponseTranslator.ConvertGameTagOptionsObjectToTagCategories(
                        response.value.data);

                    // Add tags to cache (This will last for the session)
                    ResponseCache.AddTagsToCache(tags);
                }
            }

            // FINAL SUCCESS / FAILURE depending on callback params set previously
            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return ResultAnd.Create(result, tags);
        }


        public static async void GetGameTags(Action<ResultAnd<TagCategory[]>> callback)
        {
            ResultAnd<TagCategory[]> result = await GetGameTags();
            callback(result);
        }


        public static async Task<ResultAnd<ModPage>> GetMods(SearchFilter filter)
        {           
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback params ]------------------------------
            Result result;
            ModPage page = new ModPage();
            //-------------------------------------------------------------------------------------
            string referenceURL = API.Requests.GetMods.URL_Unpaginated(filter);
            int offset = filter.pageIndex * filter.pageSize;

            if(IsInitialized(out result) && IsSearchFilterValid(filter, out result)
                                         && AreCredentialsValid(false, out result)
                                         && !ResponseCache.GetModsFromCache(referenceURL, offset, filter.pageSize, out page))
            {
                //      Synchronous checks SUCCEEDED

                string url = API.Requests.GetMods.URL_Paginated(filter);

                // MAKE RESTAPI REQUEST
                Task<ResultAnd<GetMods.ResponseSchema>> task =
                    RESTAPI.Request<GetMods.ResponseSchema>(url, API.Requests.GetMods.Template);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<GetMods.ResponseSchema> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(response.result.Succeeded())
                {
                    // Convert the ModObject response into ModProfiles
                    page = ResponseTranslator.ConvertResponseSchemaToModPage(response.value, filter);

                    // Add the ModProfiles to the cache
                    // ResponseCache.AddModsToCache(url, offset, page);

                    // Return the exact number of mods that were requested (not more)
                    if(page.modProfiles.Length > filter.pageSize)
                    {
                        Array.Copy(page.modProfiles, page.modProfiles, filter.pageSize);
                    }
                }
            }

            // FINAL SUCCESS / FAILURE depending on callback params set previously
            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return ResultAnd.Create(result, page);
        }


        public static async void GetMods(SearchFilter filter, Action<Result, ModPage> callback)
        {
            // Early out
            if(callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetMods method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            ResultAnd<ModPage> result = await GetMods(filter);
            callback?.Invoke(result.result, result.value);
        }


        public static async Task<ResultAnd<ModProfile>> GetMod(long id)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback params ]------------------------------
            Result result;
            ModProfile profile = default;
            //-------------------------------------------------------------------------------------

            // generate endpoint here because it's synchronous and we can check validity early on

            if(ModIOUnityImplementation.IsInitialized(out result)
               && AreCredentialsValid(false, out result)
               && !ResponseCache.GetModFromCache((ModId)id, out profile))
            {
                //      Synchronous checks SUCCEEDED

                string url = API.Requests.GetMod.URL(id);

                // MAKE RESTAPI REQUEST
                Task<ResultAnd<ModObject>> task =
                    RESTAPI.Request<ModObject>(url, API.Requests.GetMod.Template);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<ModObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(response.result.Succeeded())
                {
                    // Convert ModObject to ModProfile
                    profile = ResponseTranslator.ConvertModObjectToModProfile(response.value);

                    // Add ModProfile to cache
                    ResponseCache.AddModToCache(profile);
                }
            }

            // FINAL SUCCESS / FAILURE depending on callback params set previously
            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return ResultAnd.Create(result, profile);
        }

        public static async Task GetMod(long id, Action<ResultAnd<ModProfile>> callback)
        {
            // Early out
            if(callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetMod method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }
            ResultAnd<ModProfile> result = await GetMod(id);
            callback?.Invoke(result);
        }

#endregion // Mod Browsing

#region Mod Management

        public static Result EnableModManagement(
            [CanBeNull] ModManagementEventDelegate modManagementEventDelegate)
        {
            if(IsInitialized(out Result result) && AreCredentialsValid(true, out result))
            {
                ModManagement.modManagementEventDelegate = modManagementEventDelegate;
                ModManagement.EnableModManagement();
            }

            return result;
        }
#pragma warning disable 4014
        public static Result DisableModManagement()
        {
            if(IsInitialized(out Result result) && AreCredentialsValid(true, out result))
            {
                ModManagement.DisableModManagement();
                
                ModManagement.ShutdownOperations();
            }

            return result;
        }
#pragma warning restore 4014

        public static async Task<Result> FetchUpdates()
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            if(IsInitialized(out Result result) && AreCredentialsValid(true, out result))
            {
                // This syncs the user's subscribed mods and also looks for modfile changes to
                // update
                Task<Result> task = ModCollectionManager.FetchUpdates();

                openCallbacks[callbackConfirmation] = task;
                result = await task;
                openCallbacks[callbackConfirmation] = null;

                if(result.Succeeded())
                {
                    ModManagement.WakeUp();
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }


        public static async Task FetchUpdates(Action<Result> callback)
        {
            if(callback == null)
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
            if(IsInitialized(out Result result) && AreCredentialsValid(true, out result))
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
#endregion // Mod Management

#region User Management

        public static async Task<Result> AddModRating(ModId modId, ModRating rating)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            if(IsInitialized(out result) && AreCredentialsValid(true, out result))
            {
                //      Synchronous checks SUCCEEDED

                // Get endpoint and form data
                string url = API.Requests.AddModRating.URL(modId, rating, out WWWForm form);

                // MAKE RESTAPI REQUEST
                Task<ResultAnd<MessageObject>> task =
                    RESTAPI.Request<MessageObject>(url, API.Requests.AddModRating.Template, form);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<MessageObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(result.code_api == ResultCode.RESTAPI_ModRatingAlreadyExists
                   || result.code_api == ResultCode.RESTAPI_ModRatingNotFound)
                {
                    // SUCCEEDED
                    result = ResultBuilder.Success;
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);
            return result;
        }


        public static async void AddModRating(ModId modId, ModRating rating,
                                              Action<Result> callback)
        {
            // Callback warning
            if(callback == null)
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
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            UserProfile userProfile = default;
            //-------------------------------------------------------------------------------------

            string url = GetAuthenticatedUser.URL();

            if(IsInitialized(out result) && AreCredentialsValid(true, out result)
                                         && !ResponseCache.GetUserProfileFromCache(out userProfile))
            {
                //      Synchronous checks SUCCEEDED

                // MAKE RESTAPI REQUEST

                Task<ResultAnd<UserObject>> task =
                    RESTAPI.Request<UserObject>(url, GetAuthenticatedUser.Template);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<UserObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(response.result.Succeeded())
                {
                    await UserData.instance.SetUserObject(response.value);

                    // Convert UserObject to UsePrrofile
                    userProfile = ResponseTranslator.ConvertUserObjectToUserProfile(response.value);

                    // Add UserProfile to cache (lasts for the whole session)
                    ResponseCache.AddUserToCache(userProfile);
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return ResultAnd.Create(result, userProfile);
        }

        public static async Task GetCurrentUser(Action<ResultAnd<UserProfile>> callback)
        {
            // Early out
            if(callback == null)
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
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            if(IsInitialized(out result) && AreCredentialsValid(true, out result))
            {
                //      Synchronous checks SUCCEEDED

                string url = UnsubscribeFromMod.URL(modId);

                // MAKE RESTAPI REQUEST
                Task<Result> task = RESTAPI.Request(url, UnsubscribeFromMod.Template);

                openCallbacks[callbackConfirmation] = task;
                result = await task;
                openCallbacks[callbackConfirmation] = null;

                var success = result.Succeeded()
                   || result.code_api == ResultCode.RESTAPI_ModSubscriptionNotFound;

                if(success)
                {
                    result = ResultBuilder.Success;
                    ModCollectionManager.RemoveModFromUserSubscriptions(modId, false);

                    if(ShouldAbortDueToDownloading(modId))
                    {
                        ModManagement.AbortCurrentDownloadJob();
                    }
                    else if(ShouldAbortDueToInstalling(modId))
                    {
                        ModManagement.AbortCurrentInstallJob();
                    }
                    ModManagement.WakeUp();
                }

                ModCollectionManager.RemoveModFromUserSubscriptions(modId, success);
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);
            return result;
        }

        private static bool ShouldAbortDueToDownloading(ModId modId)
        {
            return ModManagement.currentJob != null 
                   && ModManagement.currentJob.mod.modObject.id == modId
                   && ModManagement.currentJob.type == ModManagementOperationType.Download;
        }

        private static bool ShouldAbortDueToInstalling(ModId modId)
        {
            return ModManagement.currentJob != null
                && ModManagement.currentJob.mod.modObject.id == modId
                && ModManagement.currentJob.type == ModManagementOperationType.Install
                && ModManagement.currentJob.zipOperation != null;
        }

        public static async void UnsubscribeFrom(ModId modId, Action<Result> callback)
        {
            if(callback == null)
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
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            if(IsInitialized(out result) && AreCredentialsValid(true, out result))
            {
                //      Synchronous checks SUCCEEDED

                string url = SubscribeToMod.URL(modId);

                // MAKE RESTAPI REQUEST
                Task<ResultAnd<ModObject>> task =
                    RESTAPI.Request<ModObject>(url, SubscribeToMod.Template);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<ModObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(result.Succeeded())
                {
                    ModCollectionManager.UpdateModCollectionEntry(modId, response.value);
                    ModCollectionManager.AddModToUserSubscriptions(modId);
                    ModManagement.WakeUp();
                }
                else if(result.code_api == ResultCode.RESTAPI_ModSubscriptionAlreadyExists)
                {
                    // Hack implementation:
                    // If sub exists, then we don't receive the Mod Object
                    // So, our sub request did nothing.
                    // If the we attempt to fetch the Mod Object, and it fails,
                    // treat the subscribe attempt as a failure.

                    result = ResultBuilder.Success;
                    ModCollectionManager.AddModToUserSubscriptions(modId);

                    url = API.Requests.GetMod.URL(modId);

                    task = RESTAPI.Request<ModObject>(url, API.Requests.GetMod.Template);

                    openCallbacks[callbackConfirmation] = task;
                    response = await task;
                    openCallbacks[callbackConfirmation] = null;

                    if(response.result.Succeeded())
                    {
                        ModCollectionManager.UpdateModCollectionEntry(modId, response.value);
                        ModManagement.WakeUp();
                    }

                    result = response.result;
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }

        public static async void SubscribeTo(ModId modId, Action<Result> callback)
        {
            if(callback == null)
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

        public static async void GetUserSubscriptions(SearchFilter filter,
                                                      Action<Result, ModProfile[], int> callback)
        {
            // Early out
            if(callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetUserSubscriptionsFromModio method, any response "
                    + "returned from the server wont be used. This operation  has been cancelled.");
                return;
            }

            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback params ]------------------------------
            Result result;
            ModPage page = new ModPage();
            //-------------------------------------------------------------------------------------

            if(IsInitialized(out result) && IsSearchFilterValid(filter, out result)
                                         && AreCredentialsValid(true, out result))
            {
                //      Synchronous checks SUCCEEDED

                string url = API.Requests.GetUserSubscriptions.URL(filter);

                Task<ResultAnd<GetUserSubscriptions.ResponseSchema>> task =
                    RESTAPI.Request<GetUserSubscriptions.ResponseSchema>(
                        url, API.Requests.GetUserSubscriptions.Template);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<GetUserSubscriptions.ResponseSchema> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(response.result.Succeeded())
                {
                    page = ResponseTranslator.ConvertResponseSchemaToModPage(response.value, filter);
                }
            }

            // FINAL SUCCESS / FAILURE depending on callback params set previously
            callback?.Invoke(result, page.modProfiles, (int)page.totalSearchResultsFound);
            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);
        }

        public static SubscribedMod[] GetSubscribedMods(out Result result)
        {
            if(IsInitialized(out result) && AreCredentialsValid(true, out result))
            {
                SubscribedMod[] mods = ModCollectionManager.GetSubscribedModsForUser(out result);
                return mods;
            }

            return null;
        }

        public static InstalledMod[] GetInstalledMods(out Result result)
        {
            if(IsInitialized(out result) && AreCredentialsValid(false, out result))
            {
                InstalledMod[] mods = ModCollectionManager.GetInstalledMods(out result);
                return mods;
            }

            return null;
        }

        public static UserInstalledMod[] GetInstalledModsForUser(out Result result)
        {
            //Filter for user
            if(IsInitialized(out result) && AreCredentialsValid(false, out result))
            {
                var mods = ModCollectionManager.GetInstalledMods(out result);
                return FilterInstalledModsIntoUserInstalledMods(UserData.instance.userObject.id, mods);
            }

            return null;
        }

        public static UserInstalledMod[] FilterInstalledModsIntoUserInstalledMods(long userId, params InstalledMod[] mods)
            => mods.Select(x => x.AsInstalledModsUser(userId))
                   .Where(x => !x.Equals(default(UserInstalledMod)))
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

#endregion // User Management

#region Mod Media


        public static async Task<ResultAnd<Texture2D>> DownloadTexture(DownloadReference downloadReference)
        {

            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback params ]------------------------------
            Result result;
            Texture2D texture = null;
            //-------------------------------------------------------------------------------------

            if(downloadReference.IsValid())
            {
                if(IsInitialized(out result) && AreCredentialsValid(false, out result))
                {
                    // Check cache asynchronously for texture in temp folder
                    Task<ResultAnd<Texture2D>> cacheTask =
                        ResponseCache.GetTextureFromCache(downloadReference);

                    openCallbacks[callbackConfirmation] = cacheTask;
                    ResultAnd<Texture2D> cacheResponse = await cacheTask;
                    openCallbacks[callbackConfirmation] = null;

                    if(cacheResponse.result.Succeeded())
                    {
                        // CACHE SUCCEEDED

                        result = cacheResponse.result;
                        texture = cacheResponse.value;
                    }
                    else
                    {
                        // MAKE RESTAPI REQUEST
                        Task<ResultAnd<Texture2D>> task =
                            RESTAPI.Request<Texture2D>(downloadReference.url, DownloadImage.Template);

                        openCallbacks[callbackConfirmation] = task;
                        ResultAnd<Texture2D> response = await task;
                        openCallbacks[callbackConfirmation] = null;

                        result = response.result;

                        if(response.result.Succeeded())
                        {
                            texture = response.value;

                            await ResponseCache.AddTextureToCache(downloadReference, response.value);
                        }
                    }
                    // continue to invoke at the end of this method
                }
            }
            else
            {
                Logger.Log(
                    LogLevel.Warning,
                    "The DownloadReference provided for the DownloadTexture method was not"
                    + " valid. Consider using the DownloadReference.IsValid() method to check if the"
                    + "DownloadReference has an existing URL before using this method.");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_DownloadReferenceIsntValid);
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return ResultAnd.Create(result, texture);
        }

        public static async void DownloadTexture(DownloadReference downloadReference,
                                                 Action<ResultAnd<Texture2D>> callback)
        {
            // Early out
            if(callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the DownloadTexture method. This operation has been cancelled.");
                return;
            }

            ResultAnd<Texture2D> result = await DownloadTexture(downloadReference);
            callback?.Invoke(result);
        }

#endregion // Mod Media

#region Reporting

        public static async Task<Result> Report(Report report)
        {

            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result = ResultBuilder.Unknown;
            //-------------------------------------------------------------------------------------

            if(report == null || !report.CanSend())
            {
                Logger.Log(
                    LogLevel.Error,
                    "The Report instance provided to the Reporting method is not setup correctly"
                    + " and cannot be sent as a valid report to mod.io");
                result = report == null
                    ? ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull)
                    : ResultBuilder.Create(ResultCode.InvalidParameter_ReportNotReady);
            }
            else if(IsInitialized(out result))
            {
                string url = API.Requests.Report.URL(report, out WWWForm form);

                Task<ResultAnd<MessageObject>> task =
                    RESTAPI.Request<MessageObject>(url, API.Requests.Report.Template, form);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<MessageObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;
            }


            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }

        public static async void Report(Report report, Action<Result> callback)
        {
            // TODO @Steve implement reporting for users
            // This has to be done before GDK and XDK implementation is publicly supported

            if(callback == null)
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
            // Check callback
            // Check disableUploads
            if(Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error,
                    "The current plugin configuration has uploading disabled.");

                return ResultAnd.Create(ResultBuilder.Create(ResultCode.Settings_UploadsDisabled), ModId.Null);
            }


            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result = ResultBuilder.Unknown;
            ModId modId = (ModId)0;
            //-------------------------------------------------------------------------------------

            // Check valid token
            if(!ModManagement.IsCreationTokenValid(token))
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
                if(IsInitialized(out result) && AreCredentialsValid(true, out result)
                                             && IsModProfileDetailsValid(modDetails, out result))
                {
                    //      Synchronous checks SUCCEEDED

                    string url = AddMod.URL(modDetails, out WWWForm form);

                    // MAKE RESTAPI REQUEST
                    Task<ResultAnd<ModObject>> task =
                        RESTAPI.Request<ModObject>(url, AddMod.Template, form);

                    openCallbacks[callbackConfirmation] = task;
                    ResultAnd<ModObject> response = await task;
                    openCallbacks[callbackConfirmation] = null;

                    result = response.result;

                    if(result.Succeeded())
                    {
                        // Succeeded
                        modId = (ModId)response.value.id;

                        ModManagement.InvalidateCreationToken(token);
                    }
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);
            return ResultAnd.Create(result, modId);
        }

        public static async void CreateModProfile(CreationToken token, ModProfileDetails modDetails,
                                                  Action<ResultAnd<ModId>> callback)
        {
            // - Early Outs -
            // Check callback
            if(callback == null)
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
            if(Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error,
                    "The current plugin configuration has uploading disabled.");

                return ResultBuilder.Create(ResultCode.Settings_UploadsDisabled);
            }

            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            // Check for modId

            if(modDetails == null)
            {
                Logger.Log(LogLevel.Error,
                    "The ModProfileDetails provided is null. You cannot update a mod "
                    + "without providing a valid ModProfileDetails object.");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }
            else if(modDetails.modId == null)
            {
                Logger.Log(
                    LogLevel.Error,
                    "The provided ModProfileDetails has not been assigned a ModId. Ensure"
                    + " you assign the Id of the mod you intend to edit to the ModProfileDetails.modId"
                    + " field.");
                result = ResultBuilder.Create(
                    ResultCode.InvalidParameter_ModProfileRequiredFieldsNotSet);
            }
            else if(IsInitialized(out result) && AreCredentialsValid(true, out result)
                                              && IsModProfileDetailsValidForEdit(modDetails, out result))
            {
                //      Synchronous checks SUCCEEDED

                string url = EditMod.URL(modDetails, out WWWForm form);

                // MAKE RESTAPI REQUEST
                Task<ResultAnd<ModObject>> task =
                    RESTAPI.Request<ModObject>(url, EditMod.Template, form);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<ModObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(result.Succeeded())
                {
                    // Succeeded
                }
                else
                {
                    // Failed
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }


        public static async void EditModProfile(ModProfileDetails modDetails,
                                                Action<Result> callback)
        {
            // Check callback
            if(callback == null)
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

        public static async void DeleteTags(ModId modId, string[] tags,
                                         Action<Result> callback)
        {
            // Check callback
            if(callback == null)
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
            if(Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error,
                    "The current plugin configuration has uploading disabled.");

                return ResultBuilder.Create(ResultCode.Settings_UploadsDisabled);
            }

            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            // Check for modId

            if(modId == 0)
            {
                Logger.Log(LogLevel.Error,
                    "You must provide a valid mod id to delete tags.");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_MissingModId);
            }
            else if(tags == null || tags.Length == 0)
            {
                Logger.Log(
                    LogLevel.Error,
                    "You must provide tags to be deleted from the mod");
                result = ResultBuilder.Create(
                    ResultCode.InvalidParameter_CantBeNull);
            }
            else if(IsInitialized(out result) && AreCredentialsValid(true, out result))
            {
                //      Synchronous checks SUCCEEDED

                string url = DeleteModTags.URL(modId, tags, out WWWForm form);

                // MAKE RESTAPI REQUEST
                Task<ResultAnd<MessageObject>> task =
                    RESTAPI.Request<MessageObject>(url, AddModTags.Template, form);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<MessageObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(result.Succeeded())
                {
                    // Succeeded
                }
                else
                {
                    // Failed
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }

        public static async void AddTags(ModId modId, string[] tags,
                                                Action<Result> callback)
        {
            // Check callback
            if(callback == null)
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
            if(Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error,
                    "The current plugin configuration has uploading disabled.");

                return ResultBuilder.Create(ResultCode.Settings_UploadsDisabled);
            }

            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            // Check for modId

            if(modId == 0)
            {
                Logger.Log(LogLevel.Error,
                    "You must provide a valid mod id to add tags.");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_MissingModId);
            }
            else if(tags == null || tags.Length == 0)
            {
                Logger.Log(
                    LogLevel.Error,
                    "You must provide tags to be added to the mod");
                result = ResultBuilder.Create(
                    ResultCode.InvalidParameter_CantBeNull);
            }
            else if(IsInitialized(out result) && AreCredentialsValid(true, out result))
            {
                //      Synchronous checks SUCCEEDED

                string url = AddModTags.URL(modId, tags, out WWWForm form);

                // MAKE RESTAPI REQUEST
                Task<ResultAnd<MessageObject>> task =
                    RESTAPI.Request<MessageObject>(url, AddModTags.Template, form);

                openCallbacks[callbackConfirmation] = task;
                ResultAnd<MessageObject> response = await task;
                openCallbacks[callbackConfirmation] = null;

                result = response.result;

                if(result.Succeeded())
                {
                    // Succeeded
                }
                else
                {
                    // Failed
                }
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

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

            ProgressHandle handle = new ProgressHandle();
            currentUploadHandle = handle;
            currentUploadHandle.OperationType = ModManagementOperationType.Upload;

            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            if (IsInitialized(out result) && AreCredentialsValid(true, out result)
               && IsModProfileDetailsValidForEdit(modProfileDetails, out result))
            {
                Task<ResultAnd<AddModMedia.AddModMediaUrlResult>> urlResultTask =
                    AddModMedia.URL(modProfileDetails);

                openCallbacks[callbackConfirmation] = urlResultTask;
                ResultAnd<AddModMedia.AddModMediaUrlResult> urlResult = await urlResultTask;
                openCallbacks[callbackConfirmation] = null;

                if(urlResult.result.Succeeded()) 
                {
                    Task<ResultAnd<ModMediaObject>> task = RESTAPI.Request<ModMediaObject>(
                        urlResult.value.url, AddModMedia.Template, urlResult.value.form, null, currentUploadHandle);

                    openCallbacks[callbackConfirmation] = task;
                    ResultAnd<ModMediaObject> response = await task;
                    openCallbacks[callbackConfirmation] = null;

                    result = response.result;

                    if(!result.Succeeded())
                    {
                        currentUploadHandle.Failed = true;
                    }
                }

            }

            currentUploadHandle.Completed = true;
            currentUploadHandle = null;

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }

        public static async Task<Result> UploadModfile(ModfileDetails modfile)
        {
            // - Early outs -
            // Check Modfile
            if(modfile == null)
            {
                Logger.Log(LogLevel.Error, "ModfileDetails parameter cannot be null.");

                return ResultBuilder.Create(ResultCode.InvalidParameter_CantBeNull);
            }

            // Check mod id
            if(modfile.modId == null)
            {
                Logger.Log(
                    LogLevel.Error,
                    "The provided ModfileDetails has not been assigned a ModId. Ensure"
                    + " you assign the Id of the mod you intend to edit to the ModProfileDetails.modId"
                    + " field.");

                return ResultBuilder.Create(ResultCode.InvalidParameter_MissingModId);
            }

            // Check disableUploads
            if(Settings.server.disableUploads)
            {
                Logger.Log(LogLevel.Error,
                    "The current plugin configuration has uploading disabled.");

                return ResultBuilder.Create(ResultCode.Settings_UploadsDisabled);
            }

            ProgressHandle handle = new ProgressHandle();
            currentUploadHandle = handle;
            currentUploadHandle.OperationType = ModManagementOperationType.Upload;

            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            if(IsInitialized(out result) && AreCredentialsValid(true, out result)
                                         && IsModfileDetailsValid(modfile, out result))
            {
                CompressOperationDirectory compressOperation = new CompressOperationDirectory(modfile.directory);

                Task<ResultAnd<MemoryStream>> compressTask = compressOperation.Compress();

                openCallbacks[callbackConfirmation] = compressTask;
                ResultAnd<MemoryStream> compressResult = await compressTask;
                openCallbacks[callbackConfirmation] = null;

                result = compressResult.result;

                if(!result.Succeeded())
                {
                    //      Compression FAILED

                    currentUploadHandle.Failed = true;

                    Logger.Log(LogLevel.Error, "Failed to compress the files at the "
                                               + $"given directory ({modfile.directory}).");
                }
                else
                {
                    //      Compression SUCCEEDED

                    string url =
                        AddModfile.URL(modfile, compressResult.value.ToArray(), out WWWForm form);

                    // MAKE RESTAPI REQUEST
                    Task<ResultAnd<ModfileObject>> task = RESTAPI.Request<ModfileObject>(
                        url, AddModfile.Template, form, null, currentUploadHandle);

                    openCallbacks[callbackConfirmation] = task;
                    ResultAnd<ModfileObject> response = await task;
                    openCallbacks[callbackConfirmation] = null;

                    result = response.result;

                    if(!result.Succeeded())
                    {
                        currentUploadHandle.Failed = true;
                    }
                }
            }

            currentUploadHandle.Completed = true;
            currentUploadHandle = null;

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }

        public static async void UploadModMedia(ModProfileDetails modProfileDetails, Action<Result> callback)
        {
            // Check for callback
            if(callback == null)
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

        public static async void UploadModfile(ModfileDetails modfile, Action<Result> callback)
        {
            // Check for callback
            if(callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the UploadModfile method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            Result result = await UploadModfile(modfile);
            callback?.Invoke(result);
        }

        public static async Task<Result> ArchiveModProfile(ModId modId)
        {
            TaskCompletionSource<bool> callbackConfirmation = new TaskCompletionSource<bool>();
            openCallbacks.Add(callbackConfirmation, null);

            //------------------------------[ Setup callback param ]-------------------------------
            Result result;
            //-------------------------------------------------------------------------------------

            if(IsInitialized(out result) && AreCredentialsValid(true, out result))
            {
                //      Synchronous checks SUCCEEDED

                string url = DeleteMod.URL(modId);

                // MAKE RESTAPI REQUEST
                Task<Result> task = RESTAPI.Request(url, DeleteMod.Template);

                // We always cache the task while awaiting so we can check IsFaulted externally
                openCallbacks[callbackConfirmation] = task;
                result = await task;
                openCallbacks[callbackConfirmation] = null;
            }

            callbackConfirmation.SetResult(true);
            openCallbacks.Remove(callbackConfirmation);

            return result;
        }

        public static async void ArchiveModProfile(ModId modId, Action<Result> callback)
        {
            if(callback == null)
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
            if(!DataStorage.TryGetModfileDetailsDirectory(modfile.directory,
                out string notbeingusedhere))
            {
                Logger.Log(LogLevel.Error,
                    "The provided directory in ModfileDetails could not be found or"
                    + $" does not exist ({modfile.directory}).");
                result = ResultBuilder.Create(ResultCode.IO_DirectoryDoesNotExist);
                return false;
            }

            // check metadata isn't too large
            if(modfile.metadata?.Length > 50000)
            {
                Logger.Log(LogLevel.Error,
                    "The provided metadata in ModProfileDetails exceeds 50,000 characters"
                    + $"(Was given {modfile.metadata.Length})");
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
            if(modDetails.logo == null || string.IsNullOrWhiteSpace(modDetails.summary)
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
            if(modDetails.summary?.Length > 250)
            {
                Logger.Log(LogLevel.Error,
                    "The provided summary in ModProfileDetails exceeds 250 characters");
                result = ResultBuilder.Create(ResultCode.InvalidParameter_ModSummaryTooLarge);
                return false;
            }

            if(modDetails.logo != null)
            {
                if(modDetails.logo.EncodeToPNG().Length > 8388608)
                {
                    Logger.Log(LogLevel.Error,
                               "The provided logo in ModProfileDetails exceeds 8 megabytes");
                    result = ResultBuilder.Create(ResultCode.InvalidParameter_ModLogoTooLarge);
                    return false;
                }
            }

            if(modDetails.metadata?.Length > 50000)
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

        public static Task<ResultAnd<ModProfile[]>> GetCurrentUserCreations()
        {
            throw new NotImplementedException();
        }

        public static async void GetCurrentUserCreations(Action<ResultAnd<ModProfile[]>> callback)
        {
            // Check for callback
            if(callback == null)
            {
                Logger.Log(
                    LogLevel.Warning,
                    "No callback was given to the GetCurrentUserCreations method. You will not "
                    + "be informed of the result for this action. It is highly recommended to "
                    + "provide a valid callback.");
            }

            ResultAnd<ModProfile[]> result = await GetCurrentUserCreations();
            callback?.Invoke(result);
        }
#endregion // Mod Uplaoding
    }
}
