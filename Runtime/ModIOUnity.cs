using ModIO.Implementation;
using System;
using JetBrains.Annotations;
using UnityEngine;
using System.Threading.Tasks;

#pragma warning disable 4014 // Ignore warnings about calling async functions from non-async code

namespace ModIO
{

    /// <summary>Main interface for the mod.io Unity plugin.</summary>
    public static class ModIOUnity
    {
#region Initialization and Maintenance

        /// <summary>
        /// You can use this to quickly identify whether or not the plugin has been initialized.
        /// </summary>
        /// <returns>true if the plugin is initialized</returns>
        /// <code>
        /// void Example()
        /// {
        ///     if (ModIOUnity.IsInitialized())
        ///     {
        ///         Debug.Log("The plugin is initialized");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("The plugin is not initialized");
        ///     }
        /// }
        /// </code>
        public static bool IsInitialized()
        {
            return ModIOUnityImplementation.isInitialized;
        }
        
        /// <summary>
        /// Assigns the logging delegate the plugin uses to output log messages that otherwise go to
        /// UnityEngine.Debug.Log(string)
        /// </summary>
        /// <remarks>
        /// If you don't wish to see [mod.io] logs appearing in the Unity console you can set your
        /// own delegate for handling logs and ignore them or display them elsewhere.
        /// </remarks>
        /// <param name="loggingDelegate">The delegate for receiving log messages</param>
        /// <seealso cref="LogMessageDelegate"/>
        /// <seealso cref="LogLevel"/>
        /// <code>
        /// void Example()
        /// {
        ///     // Send logs to MyLoggingDelegate instead of Debug.Log
        ///     ModIO.SetLoggingDelegate(MyLoggingDelegate);
        /// }
        ///
        /// public void MyLoggingDelegate(LogLevel logLevel, string logMessage)
        /// {
        ///     // Handle the log entry
        ///     if (logLevel == LogLevel.Error)
        ///     {
        ///         Debug.Log("We received an error with message: " + logMessage);
        ///     }
        /// }
        /// </code>
        public static void SetLoggingDelegate(LogMessageDelegate loggingDelegate)
        {
            ModIOUnityImplementation.SetLoggingDelegate(loggingDelegate);
        }

        /// <summary>
        /// Initializes the Plugin using the provided settings for a specified user. Loads the
        /// local state of mods installed on the system as well as relevant mods to the user. Loads the
        /// state of mods installed on the system as well as the set of mods the
        /// specified user has installed on this device.
        /// </summary>
        /// <param name="userProfileIdentifier">Name of the directory to store the user's data
        /// in.</param>
        /// <param name="serverSettings">Data used by the plugin to connect with the mod.io
        /// service.</param>
        /// <param name="buildSettings">Data used by the plugin to interact with the
        /// platform.</param>
        /// <param name="callback">Callback to invoke once the initialization is complete.</param>
        /// <seealso cref="FetchUpdates"/>
        /// <seealso cref="ServerSettings"/>
        /// <seealso cref="BuildSettings"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="Shutdown"/>
        /// <code>
        /// void Example()
        /// {
        ///     // Setup a ServerSettings struct
        ///     ServerSettings serverSettings = new ServerSettings();
        ///     serverSettings.serverURL = "https://api.test.mod.io/v1";
        ///     serverSettings.gameId = 1234;
        ///     serverSettings.gameKey = "1234567890abcdefghijklmnop";
        ///
        ///     // Setup a BuildSettings struct
        ///     BuildSettings buildSettings = new BuildSettings();
        ///     buildSettings.LogLevel = LogLevel.Verbose;
        ///     buildSettings.UserPortal = UserPortal.None;
        ///     buildSettings.requestCacheLimitKB = 0; // No limit
        /// 
        ///     ModIOUnity.InitializeForUserAsync("ExampleUser", serverSettings, buildSettings, InitializationCallback);
        /// }
        ///
        /// void InitializationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Initialized plugin");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to initialize plugin");
        ///     {
        /// }
        /// </code>
        public static void InitializeForUser(string userProfileIdentifier,
                                                  ServerSettings serverSettings,
                                                  BuildSettings buildSettings,
                                                  Action<Result> callback)
        {
            ModIOUnityImplementation.InitializeForUserAsync(userProfileIdentifier, serverSettings,
                                                            buildSettings, callback);
        }

        /// <summary>
        /// Initializes the Plugin using the provided settings for a specified user. Loads the
        /// local state of mods installed on the system as well as relevant mods to the user. Loads the
        /// state of mods installed on the system as well as the set of mods the
        /// specified user has installed on this device.
        /// </summary>
        /// <param name="userProfileIdentifier">Name of the directory to store the user's data
        /// in.</param>
        /// <param name="callback">Callback to invoke once the initialization is complete.</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="Shutdown"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModIOUnity.InitializeForUserAsync("ExampleUser", InitializationCallback);
        /// }
        ///
        /// void InitializationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Initialized plugin");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to initialize plugin");
        ///     {
        /// }
        /// </code>
        public static void InitializeForUser(string userProfileIdentifier,
                                                  Action<Result> callback)
        {
            ModIOUnityImplementation.InitializeForUserAsync(userProfileIdentifier, callback);
        }

        /// <summary>
        /// Cancels any running public operations, frees plugin resources, and invokes
        /// any pending callbacks with a cancelled result code.
        /// </summary>
        /// <remarks>
        /// Callback results invoked during a shutdown operation can be checked with
        /// Result.IsCancelled()
        /// </remarks>
        /// <seealso cref="Result"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModIOUnity.Shutdown(ShutdownCallback);
        /// }
        ///
        /// void ShutdownCallback()
        /// {
        ///     Debug.Log("Finished shutting down the ModIO Plugin");
        /// }
        /// </code>
        public static void Shutdown(Action shutdownComplete)
        {
            ModIOUnityImplementation.Shutdown(shutdownComplete);
        }

#endregion // Initialization and Maintenance

#region Authentication

        /// <summary>
        /// Sends an email with a security code to the specified Email Address. The security code
        /// is then used to Authenticate the user session using ModIOUnity.SubmitEmailSecurityCode()
        /// </summary>
        /// <remarks>
        /// The callback will return a Result object.
        /// If the email is successfully sent Result.Succeeded() will equal true.
        /// If you haven't Initialized the plugin then Result.IsInitializationError() will equal
        /// true. If the string provided for the emailaddress is not .NET compliant
        /// Result.IsAuthenticationError() will equal true.
        /// </remarks>
        /// <param name="emailaddress">the Email Address to send the security code to, eg "JohnDoe@gmail.com"</param>
        /// <param name="callback">Callback to invoke once the operation is complete</param>
        /// <seealso cref="SubmitEmailSecurityCode"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnityAsync.RequestAuthenticationEmail"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModIOUnity.RequestAuthenticationEmail("johndoe@gmail.com", RequestAuthenticationCallback);
        /// }
        ///
        /// void RequestAuthenticationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Succeeded to send security code");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to send security code to that email address");
        ///     }
        /// }
        /// </code>
        public static void RequestAuthenticationEmail(string emailaddress, Action<Result> callback)
        {
            ModIOUnityImplementation.RequestEmailAuthToken(emailaddress, callback);
        }

        /// <summary>
        /// Attempts to Authenticate the current session by submitting a security code received by
        /// email from ModIOUnity.RequestAuthenticationEmail()
        /// </summary>
        /// <remarks>
        /// It is intended that this function is used after ModIOUnity.RequestAuthenticationEmail()
        /// is performed successfully.
        /// </remarks>
        /// <param name="securityCode">The security code received from an authentication email</param>
        /// <param name="callback">Callback to invoke once the operation is complete</param>
        /// <seealso cref="RequestAuthenticationEmail"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnityAsync.SubmitEmailSecurityCode"/>
        /// <code>
        /// void Example(string userSecurityCode)
        /// {
        ///     ModIOUnity.SubmitEmailSecurityCode(userSecurityCode, SubmitCodeCallback);
        /// }
        ///
        /// void SubmitCodeCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("You have successfully authenticated the user");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to authenticate the user");
        ///     }
        /// }
        /// </code>
        public static void SubmitEmailSecurityCode(string securityCode, Action<Result> callback)
        {
            ModIOUnityImplementation.SubmitEmailSecurityCode(securityCode, callback);
        }

        /// <summary>
        /// This retrieves the terms of use text to be shown to the user to accept/deny before
        /// authenticating their account via a third party provider, eg steam or google.
        /// </summary>
        /// <remarks>
        /// If the callback succeeds it will also provide a TermsOfUse struct that contains a
        /// TermsHash struct which you will need to provide when calling a third party
        /// authentication method such as ModIOUnity.AuthenticateUserViaSteam()
        /// </remarks>
        /// <param name="serviceProvider">The provider you intend to use for authentication,
        /// eg steam, google etc. (You dont need to display terms of use to the user if they are
        /// authenticating via email security code)</param>
        /// <param name="callback">Callback to invoke once the operation is complete containing a
        /// result and a hash code to use for authentication via third party providers.</param>
        /// <seealso cref="TermsOfUse"/>
        /// <seealso cref="AuthenticateUserViaDiscord"/>
        /// <seealso cref="AuthenticateUserViaGoogle"/>
        /// <seealso cref="AuthenticateUserViaGOG"/>
        /// <seealso cref="AuthenticateUserViaItch"/>
        /// <seealso cref="AuthenticateUserViaOculus"/>
        /// <seealso cref="AuthenticateUserViaSteam"/>
        /// <seealso cref="AuthenticateUserViaSwitch"/>
        /// <seealso cref="AuthenticateUserViaXbox"/>
        /// <seealso cref="ModIOUnityAsync.GetTermsOfUse"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModIOUnity.GetTermsOfUse(GetTermsOfUseCallback);
        /// }
        ///
        /// void GetTermsOfUseCallback(ResultAnd&#60;TermsOfUse&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully retrieved the terms of use: " + response.value.termsOfUse);
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to retrieve the terms of use");
        ///     }
        /// }
        /// </code>
        public static void GetTermsOfUse(Action<ResultAnd<TermsOfUse>> callback)
        {
            ModIOUnityImplementation.GetTermsOfUse(callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the steam API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="steamToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaSteam"/>
        /// <code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// void GetTermsOfUse_Example()
        /// {
        ///     ModIOUnity.GetTermsOfUse(GetTermsOfUseCallback);
        /// }
        ///
        /// void GetTermsOfUseCallback(ResultAnd&#60;TermsOfUse&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully retrieved the terms of use: " + response.value.termsOfUse);
        ///
        ///         //  Cache the terms of use (which has the hash for when we attempt to authenticate)
        ///         modIOTermsOfUse = response.value;
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to retrieve the terms of use");
        ///     }
        /// }
        /// 
        /// // Once we have the Terms of Use and hash we can attempt to authenticate
        /// void Authenticate_Example()
        /// {
        ///     ModIOUnity.AuthenticateUserViaSteam(steamToken, "johndoe@gmail.com", modIOTermsOfUse.hash, AuthenticationCallback);
        /// }
        ///
        /// void AuthenticationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully authenticated user");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to authenticate");
        ///     }
        /// }
        /// </code>
        public static void AuthenticateUserViaSteam(string steamToken,
                                                    [CanBeNull] string emailAddress,
                                                    [CanBeNull] TermsHash? hash,
                                                    Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                steamToken, AuthenticationServiceProvider.Steam, emailAddress, hash, null, null,
                null, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the GOG API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="steamToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaGOG"/>
        /// <code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// void GetTermsOfUse_Example()
        /// {
        ///     ModIOUnity.GetTermsOfUse(GetTermsOfUseCallback);
        /// }
        ///
        /// void GetTermsOfUseCallback(ResultAnd&#60;TermsOfUse&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully retrieved the terms of use: " + response.value.termsOfUse);
        ///
        ///         //  Cache the terms of use (which has the hash for when we attempt to authenticate)
        ///         modIOTermsOfUse = response.value;
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to retrieve the terms of use");
        ///     }
        /// }
        /// 
        /// // Once we have the Terms of Use and hash we can attempt to authenticate
        /// void Authenticate_Example()
        /// {
        ///     ModIOUnity.AuthenticateUserViaGOG(gogToken, "johndoe@gmail.com", modIOTermsOfUse.hash, AuthenticationCallback);
        /// }
        ///
        /// void AuthenticationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully authenticated user");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to authenticate");
        ///     }
        /// }
        /// </code>
        public static void AuthenticateUserViaGOG(string gogToken, [CanBeNull] string emailAddress,
                                                  [CanBeNull] TermsHash? hash,
                                                  Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(gogToken, AuthenticationServiceProvider.GOG,
                                                      emailAddress, hash, null, null, null,
                                                      callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Itch.io API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="steamToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaItch"/>
        /// <code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// void GetTermsOfUse_Example()
        /// {
        ///     ModIOUnity.GetTermsOfUse(GetTermsOfUseCallback);
        /// }
        ///
        /// void GetTermsOfUseCallback(ResultAnd&#60;TermsOfUse&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully retrieved the terms of use: " + response.value.termsOfUse);
        ///
        ///         //  Cache the terms of use (which has the hash for when we attempt to authenticate)
        ///         modIOTermsOfUse = response.value;
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to retrieve the terms of use");
        ///     }
        /// }
        /// 
        /// // Once we have the Terms of Use and hash we can attempt to authenticate
        /// void Authenticate_Example()
        /// {
        ///     ModIOUnity.AuthenticateUserViaItch(itchioToken, "johndoe@gmail.com", modIOTermsOfUse.hash, AuthenticationCallback);
        /// }
        ///
        /// void AuthenticationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully authenticated user");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to authenticate");
        ///     }
        /// }
        /// </code>
        public static void AuthenticateUserViaItch(string itchioToken,
                                                   [CanBeNull] string emailAddress,
                                                   [CanBeNull] TermsHash? hash,
                                                   Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                itchioToken, AuthenticationServiceProvider.Itchio, emailAddress, hash, null, null,
                null, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Xbox API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="steamToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaXbox"/>
        /// <code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// void GetTermsOfUse_Example()
        /// {
        ///     ModIOUnity.GetTermsOfUse(GetTermsOfUseCallback);
        /// }
        ///
        /// void GetTermsOfUseCallback(ResultAnd&#60;TermsOfUse&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully retrieved the terms of use: " + response.value.termsOfUse);
        ///
        ///         //  Cache the terms of use (which has the hash for when we attempt to authenticate)
        ///         modIOTermsOfUse = response.value;
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to retrieve the terms of use");
        ///     }
        /// }
        /// 
        /// // Once we have the Terms of Use and hash we can attempt to authenticate
        /// void Authenticate_Example()
        /// {
        ///     ModIOUnity.AuthenticateUserViaXbox(xboxToken, "johndoe@gmail.com", modIOTermsOfUse.hash, AuthenticationCallback);
        /// }
        ///
        /// void AuthenticationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully authenticated user");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to authenticate");
        ///     }
        /// }
        /// </code>
        public static void AuthenticateUserViaXbox(string xboxToken,
                                                   [CanBeNull] string emailAddress,
                                                   [CanBeNull] TermsHash? hash,
                                                   Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(xboxToken, AuthenticationServiceProvider.Xbox,
                                                      emailAddress, hash, null, null, null,
                                                      callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the switch API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="steamToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaSwitch"/>
        /// <code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// void GetTermsOfUse_Example()
        /// {
        ///     ModIOUnity.GetTermsOfUse(GetTermsOfUseCallback);
        /// }
        ///
        /// void GetTermsOfUseCallback(ResultAnd&#60;TermsOfUse&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully retrieved the terms of use: " + response.value.termsOfUse);
        ///
        ///         //  Cache the terms of use (which has the hash for when we attempt to authenticate)
        ///         modIOTermsOfUse = response.value;
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to retrieve the terms of use");
        ///     }
        /// }
        /// 
        /// // Once we have the Terms of Use and hash we can attempt to authenticate
        /// void Authenticate_Example()
        /// {
        ///     ModIOUnity.AuthenticateUserViaSwitch(switchToken, "johndoe@gmail.com", modIOTermsOfUse.hash, AuthenticationCallback);
        /// }
        ///
        /// void AuthenticationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully authenticated user");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to authenticate");
        ///     }
        /// }
        /// </code>
        public static void AuthenticateUserViaSwitch(string SwitchNsaId,
                                                     [CanBeNull] string emailAddress,
                                                     [CanBeNull] TermsHash? hash,
                                                     Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                SwitchNsaId, AuthenticationServiceProvider.Switch, emailAddress, hash, null, null,
                null, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Discord API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="steamToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaDiscord"/>
        /// <code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// void GetTermsOfUse_Example()
        /// {
        ///     ModIOUnity.GetTermsOfUse(GetTermsOfUseCallback);
        /// }
        ///
        /// void GetTermsOfUseCallback(ResultAnd&#60;TermsOfUse&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully retrieved the terms of use: " + response.value.termsOfUse);
        ///
        ///         //  Cache the terms of use (which has the hash for when we attempt to authenticate)
        ///         modIOTermsOfUse = response.value;
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to retrieve the terms of use");
        ///     }
        /// }
        /// 
        /// // Once we have the Terms of Use and hash we can attempt to authenticate
        /// void Authenticate_Example()
        /// {
        ///     ModIOUnity.AuthenticateUserViaDiscord(discordToken, "johndoe@gmail.com", modIOTermsOfUse.hash, AuthenticationCallback);
        /// }
        ///
        /// void AuthenticationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully authenticated user");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to authenticate");
        ///     }
        /// }
        /// </code>
        public static void AuthenticateUserViaDiscord(string discordToken,
                                                      [CanBeNull] string emailAddress,
                                                      [CanBeNull] TermsHash? hash,
                                                      Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                discordToken, AuthenticationServiceProvider.Discord, emailAddress, hash, null, null,
                null, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Google API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="steamToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaGoogle"/>
        /// <code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// void GetTermsOfUse_Example()
        /// {
        ///     ModIOUnity.GetTermsOfUse(GetTermsOfUseCallback);
        /// }
        ///
        /// void GetTermsOfUseCallback(ResultAnd&#60;TermsOfUse&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully retrieved the terms of use: " + response.value.termsOfUse);
        ///
        ///         //  Cache the terms of use (which has the hash for when we attempt to authenticate)
        ///         modIOTermsOfUse = response.value;
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to retrieve the terms of use");
        ///     }
        /// }
        /// 
        /// // Once we have the Terms of Use and hash we can attempt to authenticate
        /// void Authenticate_Example()
        /// {
        ///     ModIOUnity.AuthenticateUserViaGoogle(googleToken, "johndoe@gmail.com", modIOTermsOfUse.hash, AuthenticationCallback);
        /// }
        ///
        /// void AuthenticationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully authenticated user");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to authenticate");
        ///     }
        /// }
        /// </code>
        public static void AuthenticateUserViaGoogle(string googleToken,
                                                     [CanBeNull] string emailAddress,
                                                     [CanBeNull] TermsHash? hash,
                                                     Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                googleToken, AuthenticationServiceProvider.Google, emailAddress, hash, null, null,
                null, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Oculus API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="steamToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaOculus"/>
        /// <code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// void GetTermsOfUse_Example()
        /// {
        ///     ModIOUnity.GetTermsOfUse(GetTermsOfUseCallback);
        /// }
        ///
        /// void GetTermsOfUseCallback(ResultAnd&#60;TermsOfUse&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully retrieved the terms of use: " + response.value.termsOfUse);
        ///
        ///         //  Cache the terms of use (which has the hash for when we attempt to authenticate)
        ///         modIOTermsOfUse = response.value;
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to retrieve the terms of use");
        ///     }
        /// }
        /// 
        /// // Once we have the Terms of Use and hash we can attempt to authenticate
        /// void Authenticate_Example()
        /// {
        ///     ModIOUnity.AuthenticateUserViaOculus(oculusDevice.Quest,
        ///                                          nonce,
        ///                                          userId,
        ///                                          oculusToken,
        ///                                          "johndoe@gmail.com",
        ///                                          modIOTermsOfUse.hash, AuthenticationCallback);
        /// }
        ///
        /// void AuthenticationCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully authenticated user");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to authenticate");
        ///     }
        /// }
        /// </code>
        public static void AuthenticateUserViaOculus(OculusDevice oculusDevice, string nonce,
                                                     long userId, string oculusToken,
                                                     [CanBeNull] string emailAddress,
                                                     [CanBeNull] TermsHash? hash,
                                                     Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                oculusToken, AuthenticationServiceProvider.Oculus, emailAddress, hash, nonce,
                oculusDevice, userId.ToString(), callback);
        }

        // TODO @Steve and @Jackson: Discuss whether to make this synchronous or not
        /// <summary>
        /// Informs you if the current user session is authenticated or not.
        /// </summary>
        /// <param name="callback"></param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnityAsync.IsAuthenticated"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModIOUnity.IsAuthenticated(IsAuthenticatedCallback);
        /// }
        ///
        /// void IsAuthenticatedCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("current session is authenticated");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("current session is not authenticated");
        ///     }
        /// }
        /// </code>
        public static void IsAuthenticated(Action<Result> callback)
        {
            ModIOUnityImplementation.IsAuthenticated(callback);
        }

        /// <summary>
        /// De-authenticates the current Mod.io user for the current session and clears all
        /// user-specific data stored on the current device. Installed mods that do not have
        /// other local users subscribed will be uninstalled if ModIOUnity.EnableModManagement() has
        /// been used to enable the mod management system.
        /// (If ModManagement is enabled).
        /// </summary>
        /// <remarks>
        /// If you dont want to erase a user be sure to use ModIOUnity.Shutdown() instead.
        /// If you re-initialize the plugin after a shutdown the user will still be authenticated.
        /// </remarks>
        /// <seealso cref="EnableModManagement(ModIO.ModManagementEventDelegate)"/>
        /// <seealso cref="Result"/>
        /// <code>
        /// //static async void Example()
        ///{
        ///    Result result = await ModIOUnity.LogOutCurrentUser();
        ///
        ///    if(result.Succeeded())
        ///    {
        ///        Debug.Log("The current user has been logged and their local data removed");
        ///    }
        ///    else
        ///    {
        ///        Debug.Log("Failed to log out the current user");
        ///    }
        ///}
        /// </code>
        public static Result LogOutCurrentUser()
        {
            return ModIOUnityImplementation.RemoveUserData();
        }

        #endregion // Authentication

#region Mod Browsing

        /// <summary>
        /// Gets the existing tags for the current game Id that can be used when searching/filtering
        /// mods.
        /// </summary>
        /// <remarks>
        /// Tags come in category groups, eg "Color" could be the name of the category and the tags
        /// themselves could be { "Red", "Blue", "Green" }
        /// </remarks>
        /// <param name="callback">the callback with the result and tags retrieved</param>
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="TagCategory"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnityAsync.GetTagCategories"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModIOUnity.GetTagCategories(GetTagsCallback);
        /// }
        ///
        /// void GetTagsCallback(ResultAnd&#60;TagCategory[]&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         foreach(TagCategory category in response.value)
        ///         {
        ///             foreach(Tag tag in category.tags)
        ///             {
        ///                 Debug.Log(tag.name + " tag is in the " + category.name + "category");
        ///             }
        ///         }
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get game tags");
        ///     }
        /// }
        /// </code>
        public static void GetTagCategories(Action<ResultAnd<TagCategory[]>> callback)
        {
            ModIOUnityImplementation.GetGameTags(callback);
        }

        /// <summary>
        /// Uses a SearchFilter to retrieve a specific Mod Page and returns the ModProfiles and
        /// total number of mods based on the Search Filter.
        /// </summary>
        /// <remarks>
        /// A ModPage contains a group of mods based on the pagination filters in SearchFilter.
        /// eg, if you use SearchFilter.SetPageIndex(0) and SearchFilter.SetPageSize(100) then
        /// ModPage.mods will contain mods from 1 to 100. But if you set SearchFilter.SetPageIndex(1)
        /// then it will have mods from 101 to 200, if that many exist.
        /// (note that 100 is the maximum page size).
        /// </remarks>
        /// <param name="filter">The filter to apply when searching through mods (also contains
        /// pagination parameters)</param>
        /// <param name="callback">callback invoked with the Result and ModPage</param>
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="ModPage"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnityAsync.GetMods"/>
        /// <code>
        /// void Example()
        /// {
        ///     SearchFilter filter = new SearchFilter();
        ///     filter.SetPageIndex(0);
        ///     filter.SetPageSize(10);
        ///     ModIOUnity.GetMods(filter, GetModsCallback);
        /// }
        ///
        /// void GetModsCallback(Result result, ModPage modPage)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("ModPage has " + modPage.mods.Length + " mods");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get mods");
        ///     }
        /// }
        /// </code>
        public static void GetMods(SearchFilter filter, Action<Result, ModPage> callback)
        {
            ModIOUnityImplementation.GetMods(filter, callback);
        }

        /// <summary>
        /// Requests a single ModProfile from the mod.io server by its ModId.
        /// </summary>
        /// <remarks>
        /// If there is a specific mod that you want to retrieve from the mod.io database you can
        /// use this method to get it.
        /// </remarks>
        /// <param name="modId">the ModId of the ModProfile to get</param>
        /// <param name="callback">callback with the Result and ModProfile</param>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModProfile"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnityAsync.GetMod"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModId modId = new ModId(1234);
        ///     ModIOUnity.GetMod(modId, GetModCallback);
        /// }
        ///
        /// void GetModCallback(ResultAnd&#60;ModProfile&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("retrieved mod " + response.value.name);
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get mod");
        ///     }
        /// }
        /// </code>
        public static void GetMod(ModId modId, Action<ResultAnd<ModProfile>> callback)
        {
            ModIOUnityImplementation.GetMod(modId.id, callback);
        }

#endregion // Mod Browsing

#region User Management

        /// <summary>
        /// Used to submit a rating for a specified mod.
        /// </summary>
        /// <remarks>
        /// This can be used to change/overwrite previous ratings of the current user.
        /// </remarks>
        /// <param name="modId">the m=ModId of the mod being rated</param>
        /// <param name="rating">the rating to give the mod. Allowed values include ModRating.Positive, ModRating.Negative, ModRating.None</param>
        /// <param name="callback">callback with the result of the request</param>
        /// <seealso cref="ModRating"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModIOUnityAsync.RateMod"/>
        /// <code>
        /// 
        /// ModProfile mod;
        /// 
        /// void Example()
        /// {
        ///     ModIOUnity.RateMod(mod.id, ModRating.Positive, RateModCallback);
        /// }
        ///
        /// void RateModCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully rated mod");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to rate mod");
        ///     {
        /// }
        /// </code>
        public static void RateMod(ModId modId, ModRating rating, Action<Result> callback)
        {
            ModIOUnityImplementation.AddModRating(modId, rating, callback);
        }

        /// <summary>
        /// Adds the specified mod to the current user's subscriptions.
        /// </summary>
        /// <remarks>
        /// If mod management has been enabled via ModIOUnity.EnableModManagement() then the mod
        /// will be downloaded and installed.
        /// </remarks>
        /// <param name="modId">ModId of the mod you want to subscribe to</param>
        /// <param name="callback">callback with the result of the request</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="EnableModManagement(ModIO.ModManagementEventDelegate)"/>
        /// <seealso cref="GetCurrentModManagementOperation"/>
        /// <seealso cref="ModIOUnityAsync.SubscribeToMod"/>
        /// <code>
        ///
        /// ModProfile mod;
        /// 
        /// void Example()
        /// {
        ///     ModIOUnity.SubscribeToMod(mod.id, SubscribeCallback);
        /// }
        ///
        /// void SubscribeCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully subscribed to mod");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to subscribe to mod");
        ///     {
        /// }
        /// </code>
        public static void SubscribeToMod(ModId modId, Action<Result> callback)
        {
            ModIOUnityImplementation.SubscribeTo(modId, callback);
        }

        /// <summary>
        /// Removes the specified mod from the current user's subscriptions.
        /// </summary>
        /// <remarks>
        /// If mod management has been enabled via ModIOUnity.EnableModManagement() then the mod
        /// will be uninstalled at the next opportunity.
        /// </remarks>
        /// <param name="modId">ModId of the mod you want to unsubscribe from</param>
        /// <param name="callback">callback with the result of the request</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="EnableModManagement(ModIO.ModManagementEventDelegate)"/>
        /// <seealso cref="GetCurrentModManagementOperation"/>
        /// <seealso cref="ModIOUnityAsync.UnsubscribeFromMod"/>
        /// <code>
        ///
        /// ModProfile mod;
        /// 
        /// void Example()
        /// {
        ///     ModIOUnity.UnsubscribeFromMod(mod.id, UnsubscribeCallback);
        /// }
        ///
        /// void UnsubscribeCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully unsubscribed from mod");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to unsubscribe from mod");
        ///     {
        /// }
        /// </code>
        public static void UnsubscribeFromMod(ModId modId, Action<Result> callback)
        {
            ModIOUnityImplementation.UnsubscribeFrom(modId, callback);
        }

        /// <summary>
        /// Retrieves all of the subscribed mods for the current user.
        /// </summary>
        /// <remarks>
        /// Note that these are not installed mods only mods the user has opted as 'subscribed'.
        /// Also, ensure you have called ModIOUnity.FetchUpdates() at least once during this session
        /// in order to have an accurate collection of the user's subscriptions.
        /// </remarks>
        /// <param name="result">an out parameter for whether or not the method succeeded</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="SubscribedMod"/>
        /// <seealso cref="FetchUpdates"/>
        /// <returns>an array of the user's subscribed mods</returns>
        /// <code>
        /// void Example()
        /// {
        ///     SubscribedMod[] mods = ModIOUnity.GetSubscribedMods(out Result result);
        /// 
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("use has " + mods.Length + " subscribed mods");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get user mods");
        ///     }
        /// }
        /// </code>
        public static SubscribedMod[] GetSubscribedMods(out Result result)
        {
            return ModIOUnityImplementation.GetSubscribedMods(out result);
        }

        /// <summary>
        /// Gets the current user's UserProfile struct. Containing their mod.io username, user id,
        /// language, timezone and download references for their avatar.
        /// </summary>
        /// <remarks>
        /// This requires the current session to have an authenticated user, otherwise
        /// Result.IsAuthenticationError() from the Result will equal true.
        /// </remarks>
        /// <param name="callback">callback with the Result and the UserProfile</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="UserProfile"/>
        /// <seealso cref="IsAuthenticated"/>
        /// <seealso cref="ModIOUnityAsync.GetCurrentUser"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModIOUnity.GetCurrentUser(GetUserCallback);
        /// }
        ///
        /// void GetUserCallback(ResultAnd&#60;UserProfile&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Got user: " + response.value.username);
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get user");
        ///     }
        /// }
        /// </code>
        public static void GetCurrentUser(Action<ResultAnd<UserProfile>> callback)
        {
            ModIOUnityImplementation.GetCurrentUser(callback);
        }

#endregion

#region Mod Management

        /// <summary>
        /// This retrieves the user's subscriptions from the mod.io server and synchronises it with
        /// our local instance of the user's subscription data. If mod management has been enabled
        /// via ModIOUnity.EnableModManagement() then it may begin to install/uninstall mods.
        /// </summary>
        /// <remarks>
        /// This requires the current session to have an authenticated user, otherwise
        /// Result.IsAuthenticationError() from the Result will equal true.
        /// </remarks>
        /// <param name="callback">callback with the Result of the operation</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="EnableModManagement(ModIO.ModManagementEventDelegate)"/>
        /// <seealso cref="IsAuthenticated"/>
        /// <seealso cref="RequestAuthenticationEmail"/>
        /// <seealso cref="SubmitEmailSecurityCode"/>
        /// <seealso cref="AuthenticateUserViaDiscord"/>
        /// <seealso cref="AuthenticateUserViaGoogle"/>
        /// <seealso cref="AuthenticateUserViaGOG"/>
        /// <seealso cref="AuthenticateUserViaItch"/>
        /// <seealso cref="AuthenticateUserViaOculus"/>
        /// <seealso cref="AuthenticateUserViaSteam"/>
        /// <seealso cref="AuthenticateUserViaSwitch"/>
        /// <seealso cref="AuthenticateUserViaXbox"/>
        /// <seealso cref="ModIOUnityAsync.FetchUpdates"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModIOUnity.FetchUpdates(FetchUpdatesCallback);
        /// }
        ///
        /// void FetchUpdatesCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("updated user subscriptions");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get user subscriptions");
        ///     }
        /// }
        /// </code>
        public static void FetchUpdates(Action<Result> callback)
        {
            ModIOUnityImplementation.FetchUpdates(callback);
        }

        /// <summary>
        /// Enables the mod management system. When enabled it will automatically download, install,
        /// update and delete mods according to the authenticated user's subscriptions.
        /// </summary>
        /// <remarks>
        /// This requires the current session to have an authenticated user, otherwise
        /// Result.IsAuthenticationError() from the Result will equal true.
        /// </remarks>
        /// <param name="modManagementEventDelegate"> A delegate that gets called everytime the ModManagement system runs an event (can be null)</param>
        /// <returns>A Result for whether or not mod management was enabled</returns>
        /// <seealso cref="Result"/>
        /// <seealso cref="DisableModManagement"/>
        /// <seealso cref="IsAuthenticated"/>
        /// <code>
        /// void Example()
        /// {
        ///     Result result = ModIOUnity.EnableModManagement(ModManagementDelegate);
        /// }
        ///
        /// void ModManagementDelegate(ModManagementEventType eventType, ModId modId)
        /// {
        ///     Debug.Log("a mod management event of type " + eventType.ToString() + " has been invoked");
        /// }
        /// </code>
        public static Result EnableModManagement(
            [CanBeNull] ModManagementEventDelegate modManagementEventDelegate)
        {
            return ModIOUnityImplementation.EnableModManagement(modManagementEventDelegate);
        }

        /// <summary>
        /// Disables the mod management system and cancels any ongoing jobs for downloading or
        /// installing mods.
        /// </summary>
        /// <code>
        /// void Example()
        /// {
        ///     Result result = ModIOUnity.DisableModManagement();
        /// 
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("disabled mod management");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to disable mod management");
        ///     }
        /// }
        /// </code>
        public static Result DisableModManagement()
        {
            return ModIOUnityImplementation.DisableModManagement();
        }

        /// <summary>
        /// Returns a ProgressHandle with information on the current mod management operation.
        /// </summary>
        /// <returns>
        /// Optional ProgressHandle object containing information regarding the progress of
        /// the operation. Null if no operation is running
        /// </returns>
        /// <seealso cref="ProgressHandle"/>
        /// <seealso cref="EnableModManagement"/>
        /// <code>
        /// void Example()
        /// {
        ///     ProgressHandle handle = ModIOUnity.GetCurrentModManagementOperation();
        /// 
        ///     if (handle != null)
        ///     {
        ///         Debug.Log("current mod management operation is " + handle.OperationType.ToString());
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("no current mod management operation");
        ///     }
        /// }
        /// </code>
        public static ProgressHandle GetCurrentModManagementOperation()
        {
            return ModIOUnityImplementation.GetCurrentModManagementOperation();
        }

        /// <summary>
        /// Gets an array of mods that are installed on the current device.
        /// </summary>
        /// <remarks>
        /// Note that these will not be subscribed by the current user. If you wish to get all
        /// of the current user's installed mods use ModIOUnity.GetSubscribedMods() and check the
        /// SubscribedMod.status equals SubscribedModStatus.Installed.
        /// </remarks>
        /// <param name="result">an out Result to inform whether or not it was able to get installed mods</param>
        /// <seealso cref="InstalledMod"/>
        /// <seealso cref="GetSubscribedMods"/>
        /// <returns>an array of InstalledMod for each existing mod installed on the current device (and not subscribed by the current user)</returns>
        /// <code>
        /// void Example()
        /// {
        ///     InstalledMod[] mods = ModIOUnity.GetSystemInstalledMods(out Result result);
        /// 
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("found " + mods.Length.ToString() + " mods installed");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get installed mods");
        ///     }
        /// }
        /// </code>
        public static InstalledMod[] GetSystemInstalledMods(out Result result)
        {
            return ModIOUnityImplementation.GetInstalledMods(out result);
        }

        /// <summary>
        /// Gets an array of mods that are installed for the current user.
        /// </summary>
        /// <param name="result">an out Result to inform whether or not it was able to get installed mods</param>
        /// <seealso cref="UserInstalledMod"/>
        /// <seealso cref="GetSubscribedMods"/>
        /// <returns>an array of InstalledModUser for each existing mod installed for the user</returns>
        /// <code>
        /// void Example()
        /// {
        ///     InstalledModUser[] mods = ModIOUnity.GetSystemInstalledModsUser(out Result result);
        /// 
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("found " + mods.Length.ToString() + " mods installed");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get installed mods");
        ///     }
        /// }
        /// </code>
        public static UserInstalledMod[] GetInstalledModsForUser(out Result result)
        {
            return ModIOUnityImplementation.GetInstalledModsForUser(out result);
        }

        /// <summary>
        /// This informs the mod management system that this mod should be uninstalled if not
        /// subscribed by the current user. (such as a mod installed by a different user not
        /// currently active).
        /// </summary>
        /// <remarks>
        /// Normally if you wish to uninstall a mod you should unsubscribe and use
        /// ModIOUnity.EnableModManagement() and the process will be handled automatically. However,
        /// if you want to uninstall a mod that is subscribed to a different user session this
        /// method will mark the mod to be uninstalled to free up disk space.
        /// Alternatively you can use ModIOUnity.RemoveUserData() to remove a user from the
        /// local registry. If no other users are subscribed to the same mod it will be uninstalled
        /// automatically.
        /// </remarks>
        /// <param name="modId">The ModId of the mod to uninstall</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="SubscribeToMod"/>
        /// <seealso cref="UnsubscribeFromMod"/>
        /// <seealso cref="EnableModManagement"/>
        /// <seealso cref="LogOutCurrentUser"/>
        /// <code>
        ///
        /// ModProfile mod;
        /// 
        /// void Example()
        /// {
        ///     Result result = ModIOUnity.ForceUninstallMod(mod.id);
        ///
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("mod marked for uninstall");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to mark mod for uninstall");
        ///     }
        /// }
        /// </code>
        public static Result ForceUninstallMod(ModId modId)
        {
            return ModIOUnityImplementation.ForceUninstallMod(modId);
        }

        /// <summary>
        /// Checks if the automatic management process is currently awake and performing a mod
        /// management operation, such as installing, downloading, uninstalling, updating.
        /// </summary>
        /// <returns>True if automatic mod management is currently performing an operation.</returns>
        /// <seealso cref="EnableModManagement"/>
        /// <seealso cref="DisableModManagement"/>
        /// <seealso cref="GetCurrentModManagementOperation"/>
        /// <code>
        /// void Example()
        /// {
        ///     if (ModIOUnity.IsModManagementBusy())
        ///     {
        ///         Debug.Log("mod management is busy");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("mod management is not busy");
        ///     }
        /// }
        /// </code>
        public static bool IsModManagementBusy()
        {
            return ModIOUnityImplementation.IsModManagementBusy();
        }


#endregion // Mod Management

#region Mod Uploading

        /// <summary>
        /// Gets a token that can be used to create a new mod profile on the mod.io server.
        /// </summary>
        /// <returns>a CreationToken used in ModIOUnity.CreateModProfile()</returns>
        /// <seealso cref="CreationToken"/>
        /// <seealso cref="ModProfileDetails"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="CreateModProfile"/>
        /// <seealso cref="EditModProfile"/>
        /// <code>
        /// void Example()
        /// {
        ///     CreationToken token = ModIOUnity.GenerateCreationToken();
        /// }
        /// </code>
        public static CreationToken GenerateCreationToken()
        {
            return ModIOUnityImplementation.GenerateCreationToken();
        }

        /// <summary>
        /// Creates a new mod profile on the mod.io server based on the details provided from the
        /// ModProfileDetails object provided.
        /// </summary>
        /// <remarks>
        /// Note that this will create a new profile on the server and can be viewed online through
        /// a browser.
        /// </remarks>
        /// <param name="token">the token allowing a new unique profile to be created from ModIOUnity.GenerateCreationToken()</param>
        /// <param name="modProfileDetails">the mod profile details to apply to the mod profile being created</param>
        /// <param name="callback">a callback with the Result of the operation and the ModId of the newly created mod profile (if successful)</param>
        /// <seealso cref="GenerateCreationToken"/>
        /// <seealso cref="CreationToken"/>
        /// <seealso cref="ModProfileDetails"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModIOUnityAsync.CreateModProfile"/>
        /// <code>
        /// ModId newMod;
        /// CreationToken token;
        /// 
        /// void Example()
        /// {
        ///     token = ModIOUnity.GenerateCreationToken();
        ///
        ///     ModProfileDetails profile = new ModProfileDetails();
        ///     profile.name = "mod name";
        ///     profile.summary = "a brief summary about this mod being submitted"
        ///
        ///     ModIOUnity.CreateModProfile(token, profile, CreateProfileCallback);
        /// }
        ///
        /// void CreateProfileCallback(ResultAnd&#60;ModId&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         newMod = response.value;
        ///         Debug.Log("created new mod profile with id " + response.value.ToString());
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to create new mod profile");
        ///     }
        /// }
        /// </code>
        public static void CreateModProfile(CreationToken token,
                                            ModProfileDetails modProfileDetails,
                                            Action<ResultAnd<ModId>> callback)
        {
            ModIOUnityImplementation.CreateModProfile(token, modProfileDetails, callback);
        }

        /// <summary>
        /// This is used to edit or change data (except images) in an existing mod profile on the
        /// mod.io server. If you want to add or edit images, use UploadModMedia.
        /// </summary>
        /// <remarks>
        /// You need to assign the ModId of the mod you want to edit inside of the ModProfileDetails
        /// object included in the parameters
        /// </remarks>
        /// <param name="modProfile">the mod profile details to apply to the mod profile being created</param>
        /// <param name="callback">a callback with the Result of the operation and the ModId of the newly created mod profile (if successful)</param>
        /// <seealso cref="ModProfileDetails"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnityAsync.EditModProfile"/>
        /// <code>
        /// ModId modId;
        /// 
        /// void Example()
        /// {
        ///     ModProfileDetails profile = new ModProfileDetails();
        ///     profile.id = modId;
        ///     profile.summary = "a new brief summary about this mod being edited";
        /// 
        ///     ModIOUnity.EditModProfile(profile, EditProfileCallback);
        /// }
        ///
        /// void EditProfileCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("edited mod profile");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to edit mod profile");
        ///     }
        /// }
        /// </code>
        public static void EditModProfile(ModProfileDetails modProfile, Action<Result> callback)
        {
            ModIOUnityImplementation.EditModProfile(modProfile, callback);
        }

        /// <summary>
        /// This will return null if no upload operation is currently being performed.
        /// </summary>
        /// <remarks>
        /// Uploads are not handled by the mod management system, these are handled separately.
        /// </remarks>
        /// <returns>A ProgressHandle informing the upload state and progress. Null if no upload operation is running.</returns>
        /// <seealso cref="UploadModfile"/>
        /// <seealso cref="ArchiveModProfile"/>
        /// <code>
        /// void Example()
        /// {
        ///     ProgressHandle handle = ModIOUnity.GetCurrentUploadHandle();
        ///
        ///     if (handle != null)
        ///     {
        ///         Debug.Log("Current upload progress is: " + handle.Progress.ToString());
        ///     }
        /// }
        /// </code>
        public static ProgressHandle GetCurrentUploadHandle()
        {
            return ModIOUnityImplementation.GetCurrentUploadHandle();
        }

        /// <summary>
        /// Used to upload a mod file to a mod profile on the mod.io server. A mod file is the
        /// actual archive of a mod. This method can be used to update a mod to a newer version
        /// (you can include changelog information in ModfileDetails).
        /// </summary>
        /// <remarks>
        /// If you want to upload images such as a new logo or gallery images, you can use
        /// UploadModMedia instead.
        /// </remarks>
        /// <param name="modfile">the mod file and details to upload</param>
        /// <param name="callback">callback with the Result of the upload when the operation finishes</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModfileDetails"/>
        /// <seealso cref="ArchiveModProfile"/>
        /// <seealso cref="GetCurrentUploadHandle"/>
        /// <seealso cref="ModIOUnityAsync.UploadModfile"/>
        /// <seealso cref="UploadModMedia"/>
        /// <code>
        /// 
        /// ModId modId;
        /// 
        /// void Example()
        /// {
        ///     ModfileDetails modfile = new ModfileDetails();
        ///     modfile.modId = modId;
        ///     modfile.directory = "files/mods/mod_123";
        /// 
        ///     ModIOUnity.UploadModfile(modfile, UploadModCallback);
        /// }
        ///
        /// void UploadModCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("uploaded mod file");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to upload mod file");
        ///     }
        /// }
        /// </code>
        public static void UploadModfile(ModfileDetails modfile, Action<Result> callback)
        {
            ModIOUnityImplementation.UploadModfile(modfile, callback);
        }

        /// <summary>
        /// This is used to update the logo of a mod or the gallery images. This works very similar
        /// to EditModProfile except it only affects the images.
        /// </summary>
        /// <param name="modProfileDetails">this holds the reference to the images you wish to upload</param>
        /// <param name="callback">a callback with the Result of the operation</param>
        /// <seealso cref="ModProfileDetails"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="EditModProfile"/>
        /// <seealso cref="ModIOUnityAsync.UploadModMedia"/>
        /// <code>
        /// ModId modId;
        /// Texture2D newTexture;
        /// 
        /// void Example()
        /// {
        ///     ModProfileDetails profile = new ModProfileDetails();
        ///     profile.id = modId;
        ///     profile.logo = newTexture;
        /// 
        ///     ModIOUnity.UploadModMedia(profile, UploadProfileCallback);
        /// }
        ///
        /// void UploadProfileCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("uploaded new mod logo");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to uploaded mod logo");
        ///     }
        /// }
        /// </code>
        public static void UploadModMedia(ModProfileDetails modProfileDetails, Action<Result> callback)
        {
            ModIOUnityImplementation.UploadModMedia(modProfileDetails, callback);
        }

        /// <summary>
        /// Removes a mod from being visible on the mod.io server.
        /// </summary>
        /// <remarks>
        /// If you want to delete a mod permanently you can do so from a web browser.
        /// </remarks>
        /// <param name="modId">the id of the mod to delete</param>
        /// <param name="callback">callback with the result of the operation</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="CreateModProfile"/>
        /// <seealso cref="EditModProfile"/>
        /// <seealso cref="ModIOUnityAsync.ArchiveModProfile"/>
        /// <code>
        /// 
        /// ModId modId;
        /// 
        /// void Example()
        /// {
        ///     ModIOUnity.ArchiveModProfile(modId, ArchiveModCallback);
        /// }
        ///
        /// void ArchiveModCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("archived mod profile");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to archive mod profile");
        ///     }
        /// }
        /// </code>
        public static void ArchiveModProfile(ModId modId, Action<Result> callback)
        {
            ModIOUnityImplementation.ArchiveModProfile(modId, callback);
        }

        /// <summary>
        /// Not implemented yet
        /// </summary>
        public static void GetCurrentUserCreations(Action<ResultAnd<ModProfile[]>> callback)
        {
            ModIOUnityImplementation.GetCurrentUserCreations(callback);
        }

        /// <summary>
        /// Adds the provided tags to the specified mod id. In order for this to work the
        /// authenticated user must have permission to edit the specified mod. Only existing tags
        /// as part of the game Id will be added.
        /// </summary>
        /// <param name="modId">Id of the mod to add tags to</param>
        /// <param name="tags">array of tags to be added</param>
        /// <param name="callback">callback with the result of the operation</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="DeleteTags"/>
        /// <seealso cref="ModIOUnityAsync.AddTags"/>
        /// <code>
        /// 
        /// ModId modId;
        /// string[] tags;
        /// 
        /// void Example()
        /// {
        ///     ModIOUnity.AddTags(modId, tags, AddTagsCallback);
        /// }
        ///
        /// void AddTagsCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("added tags");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to add tags");
        ///     }
        /// }
        /// </code>
        public static void AddTags(ModId modId, string[] tags, Action<Result> callback)
        {
            ModIOUnityImplementation.AddTags(modId, tags, callback);
        }

        /// <summary>
        /// Deletes the specified tags from the mod. In order for this to work the
        /// authenticated user must have permission to edit the specified mod.
        /// </summary>
        /// <param name="modId">the id of the mod for deleting tags</param>
        /// <param name="tags">array of tags to be deleted</param>
        /// <param name="callback">callback with the result of the operation</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="AddTags"/>
        /// <seealso cref="ModIOUnityAsync.DeleteTags"/>
        /// <code>
        /// 
        /// ModId modId;
        /// string[] tags;
        /// 
        /// void Example()
        /// {
        ///     ModIOUnity.DeleteTags(modId, tags, DeleteTagsCallback);
        /// }
        ///
        /// void DeleteTagsCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("deleted tags");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to delete tags");
        ///     }
        /// }
        /// </code>
        public static void DeleteTags(ModId modId, string[] tags, Action<Result> callback)
        {
            ModIOUnityImplementation.DeleteTags(modId, tags, callback);
        }
#endregion // Mod Uploading

#region Media Download

        /// <summary>
        /// Downloads a texture based on the specified download reference.
        /// </summary>
        /// <remarks>
        /// You can get download references from UserProfiles and ModProfiles
        /// </remarks>
        /// <param name="downloadReference">download reference for the texture (eg UserObject.avatar_100x100)</param>
        /// <param name="callback">callback with the Result and Texture2D from the download</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="DownloadReference"/>
        /// <seealso cref="Texture2D"/>
        /// <seealso cref="ModIOUnityAsync.DownloadTexture"/>
        /// <code>
        ///
        /// ModProfile mod;
        /// 
        /// void Example()
        /// {
        ///     ModIOUnity.DownloadTexture(mod.logoImage_320x180, DownloadTextureCallback);
        /// }
        ///
        /// void DownloadTextureCallback(ResultAnd&#60;Texture2D&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("downloaded the mod logo texture");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to download the mod logo texture");
        ///     }
        /// }
        /// </code>
        public static void DownloadTexture(DownloadReference downloadReference,
                                           Action<ResultAnd<Texture2D>> callback)
        {
            ModIOUnityImplementation.DownloadTexture(downloadReference, callback);
        }

#endregion // Media Download

#region Reporting

        /// <summary>
        /// Reports a specified mod to mod.io.
        /// </summary>
        /// <param name="report">the object containing all of the details of the report you are sending</param>
        /// <param name="callback">callback with the Result of the report</param>
        /// <seealso cref="Report"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnityAsync.Report"/>
        /// <code>
        /// void Example()
        /// {
        ///     Report report = new Report(new ModId(123),
        ///                                 ReportType.Generic,
        ///                                 "reporting this mod for a generic reason",
        ///                                 "JohnDoe",
        ///                                 "johndoe@mod.io");
        ///     
        ///     ModIOUnity.Report(report, ReportCallback);
        /// }
        ///
        /// void ReportCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("successfully sent a report");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to send a report");
        ///     }
        /// }
        /// </code>
        public static void Report(Report report, Action<Result> callback)
        {
            ModIOUnityImplementation.Report(report, callback);
        }

#endregion // Reporting
    }
}

#pragma warning restore 4014 // Restore warnings about calling async functions from non-async code
