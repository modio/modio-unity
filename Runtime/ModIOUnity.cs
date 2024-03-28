using ModIO.Implementation;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.API.Requests;
using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 4014 // Ignore warnings about calling async functions from non-async code

namespace ModIO
{
    /// <summary>Main interface for the mod.io Unity plugin.</summary>
    /// <remarks>Every <see cref="ModIOUnity" /> method with a callback has an asynchronous alternative in <see cref="ModIOUnityAsync" />.</remarks>
    /// <seealso cref="ModIOUnityAsync"/>
    public static class ModIOUnity
    {
        #region Initialization and Maintenance

        /// <returns><c>true</c> if the plugin has been initialized.</returns>
        public static bool IsInitialized() => ModIOUnityImplementation.isInitialized;

        /// <summary>Use to send log messages to <paramref name="loggingDelegate"/> instead of <c>Unity.Debug.Log(string)</c>.</summary>
        /// <param name="loggingDelegate">The delegate for receiving log messages</param>
        /// <example><code>
        /// ModIOUnity.SetLoggingDelegate((LogLevel logLevel, string logMessage) => {
        ///     if (logLevel == LogLevel.Error)
        ///         Debug.LogError($"mod.io plugin error: {logMessage}");
        /// });
        /// </code></example>
        /// <seealso cref="LogMessageDelegate"/>
        /// <seealso cref="LogLevel"/>
        public static void SetLoggingDelegate(LogMessageDelegate loggingDelegate) => ModIOUnityImplementation.SetLoggingDelegate(loggingDelegate);

        /// <summary><inheritdoc cref="InitializeForUser(string)" /><para>Use <see cref="InitializeForUser(string)" /> if you have a pre-configured mod.io config ScriptableObject.</para></summary>
        /// <param name="userProfileIdentifier"><inheritdoc cref="InitializeForUser(string)" path="//param[@name='nameOfParameter']/node()" /></param>
        /// <param name="serverSettings">Data used by the plugin to connect with the mod.io service.</param>
        /// <param name="buildSettings">Data used by the plugin to interact with the platform.</param>
        /// <example><code>
        /// ServerSettings serverSettings = new ServerSettings {
        ///     serverURL = "https://api.test.mod.io/v1",
        ///     gameId = 1234,
        ///     gameKey = "1234567890abcdefghijklmnop"
        /// };
        /// <br />
        ///
        /// BuildSettings buildSettings = new BuildSettings {
        ///     logLevel = LogLevel.Verbose,
        ///     userPortal = UserPortal.None,
        ///     requestCacheLimitKB = 0 // No limit
        /// };
        /// <br />
        /// Result result = ModIOUnity.InitializeForUser("default", serverSettings, buildSettings);
        /// if (result.Succeeded())
        ///     Debug.Log("Plugin initialized for default user");
        /// </code></example>
        /// <seealso cref="ServerSettings"/>
        /// <seealso cref="BuildSettings"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="FetchUpdates"/>
        /// <seealso cref="Shutdown"/>
        public static Result InitializeForUser(string userProfileIdentifier,
                                               ServerSettings serverSettings,
                                               BuildSettings buildSettings) =>
            ModIOUnityImplementation.InitializeForUser(userProfileIdentifier, serverSettings, buildSettings);

        /// <summary>Initializes the Plugin for the specified user and loads the state of mods installed on the system, as well as the subscribed mods the user has installed on this device.</summary>
        /// <param name="userProfileIdentifier">
        ///     A locally unique identifier for the current user.<br />
        ///     Can be used to cache multiple user authentications and mod-subscriptions.<br />
        ///     Use <c>"default"</c> if you only ever have one user.
        /// </param>
        /// <example><code>
        /// Result result = ModIOUnity.InitializeForUser("default");
        /// if (result.Succeeded())
        ///     Debug.Log("Plugin initialized for default user");
        /// </code></example>
        /// <seealso cref="Result"/>
        /// <seealso cref="FetchUpdates"/>
        /// <seealso cref="Shutdown"/>
        /// <code>
        /// void Example()
        /// {
        ///     ModIOUnity.InitializeForUser("ExampleUser", InitializationCallback);
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
        /// </summary>
        public static Result InitializeForUser(string userProfileIdentifier) => ModIOUnityImplementation.InitializeForUser(userProfileIdentifier);

        /// <summary>Cancels all public operations, frees plugin resources and invokes any pending callbacks with a cancelled result code.</summary>
        /// <remarks><c>Result.IsCancelled()</c> can be used to determine if it was cancelled due to a shutdown operation.</remarks>
        /// <example><code>ModIOUnity.Shutdown(() => Debug.Log("Plugin shutdown complete"));</code></example>
        /// <seealso cref="Result"/>
        public static void Shutdown(Action shutdownComplete) => ModIOUnityImplementation.Shutdown(shutdownComplete);

        #endregion // Initialization and Maintenance

        #region Authentication

        /// <summary>Listen for an external login attempt. The callback argument contains an <see cref="ExternalAuthenticationToken"/> that includes the url and code to display to the user. <c>ExternalAuthenticationToken.task</c> will complete once the user enters the code.</summary>
        /// <param name="callback">The callback to handle the response, which includes the <see cref="ExternalAuthenticationToken"/> if the request was successful.</param>
        /// <remarks>The request will time out after 15 minutes. You can cancel it at any time using <c>token.Cancel()</c>.</remarks>
        /// <example><code>
        /// ModIOUnity.RequestExternalAuthentication(async response =>
        /// {
        ///     if (!response.result.Succeeded())
        ///     {
        ///         Debug.Log($"RequestExternalAuthentication failed: {response.result.message}");
        ///
        ///         return;
        ///     }
        /// <br />
        ///     var token = response.value; // Call token.Cancel() to cancel the authentication
        /// <br />
        ///     Debug.Log($"Go to {token.url} in your browser and enter '{token.code}' to login.");
        /// <br />
        ///     Result resultToken = await token.task;
        /// <br />
        ///     Debug.Log(resultToken.Succeeded() ? "Authentication successful" : "Authentication failed (possibly timed out)");
        /// });
        /// </code></example>
        /// <seealso cref="Result"/>
        /// <seealso cref="ResultAnd"/>
        /// <seealso cref="ExternalAuthenticationToken"/>
        public static void RequestExternalAuthentication(Action<ResultAnd<ExternalAuthenticationToken>> callback) => ModIOUnityImplementation.BeginWssAuthentication(callback);

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
        /// <example><code>
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
        /// </code></example>
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
        /// <example><code>
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
        /// </code></example>
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
        /// <seealso cref="AuthenticateUserViaPlayStation"/>
        /// <seealso cref="ModIOUnityAsync.GetTermsOfUse"/>
        /// <example><code>
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
        /// </code></example>
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
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse() (Can be null if submitted once before)</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaSteam"/>
        /// <example><code>
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
        /// </code></example>
        public static void AuthenticateUserViaSteam(string steamToken,
                                                    string emailAddress,
                                                    TermsHash? hash,
                                                    Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                steamToken, AuthenticationServiceProvider.Steam, emailAddress, hash, null, null,
                null, 0, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the epic API.
        /// </summary>
        /// <param name="epicToken">the user's epic token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaEpic"/>
        public static void AuthenticateUserViaEpic(string epicToken,
                                                    string emailAddress,
                                                    TermsHash? hash,
                                                    Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                epicToken, AuthenticationServiceProvider.Epic, emailAddress, hash, null, null,
                null, 0, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the GOG API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="gogToken">the user's gog token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaGOG"/>
        /// <example><code>
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
        /// </code></example>
        public static void AuthenticateUserViaGOG(string gogToken, string emailAddress,
                                                  TermsHash? hash,
                                                  Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(gogToken, AuthenticationServiceProvider.GOG,
                                                      emailAddress, hash, null, null, null,
                                                      0, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the GOG API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="authCode">the user's auth code</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="environment">the PSN account environment</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaGOG"/>
        /// <example><code>
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
        ///     ModIOUnity.AuthenticateUserViaPlaystation(authCode, "johndoe@gmail.com", modIOTermsOfUse.hash, PlayStationEnvironment.np, AuthenticationCallback);
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
        /// </code></example>
        public static void AuthenticateUserViaPlayStation(string authCode, string emailAddress,
                                                  TermsHash? hash, PlayStationEnvironment environment,
                                                  Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(authCode, AuthenticationServiceProvider.PlayStation,
                                                      emailAddress, hash, null, null, null, environment,
                                                      callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Itch.io API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="itchioToken">the user's itch token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaItch"/>
        /// <example><code>
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
        /// </code></example>
        public static void AuthenticateUserViaItch(string itchioToken,
                                                   string emailAddress,
                                                   TermsHash? hash,
                                                   Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                itchioToken, AuthenticationServiceProvider.Itchio, emailAddress, hash, null, null,
                null, 0, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Xbox API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="xboxToken">the user's xbl token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaXbox"/>
        /// <example><code>
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
        /// </code></example>
        public static void AuthenticateUserViaXbox(string xboxToken,
                                                   string emailAddress,
                                                   TermsHash? hash,
                                                   Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(xboxToken, AuthenticationServiceProvider.Xbox,
                                                      emailAddress, hash, null, null, null,
                                                      0, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the switch API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="SwitchNsaId">the user's switch NSA id token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaSwitch"/>
        /// <example><code>
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
        /// </code></example>
        public static void AuthenticateUserViaSwitch(string SwitchNsaId,
                                                     string emailAddress,
                                                     TermsHash? hash,
                                                     Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                SwitchNsaId, AuthenticationServiceProvider.Switch, emailAddress, hash, null, null,
                null, 0, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Discord API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="discordToken">the user's discord token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaDiscord"/>
        /// <example><code>
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
        /// </code></example>
        public static void AuthenticateUserViaDiscord(string discordToken,
                                                      string emailAddress,
                                                      TermsHash? hash,
                                                      Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                discordToken, AuthenticationServiceProvider.Discord, emailAddress, hash, null, null,
                null, 0, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Google API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="googleToken">the user's google token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaGoogle"/>
        /// <example><code>
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
        /// </code></example>
        public static void AuthenticateUserViaGoogle(string googleToken,
                                                     string emailAddress,
                                                     TermsHash? hash,
                                                     Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                googleToken, AuthenticationServiceProvider.Google, emailAddress, hash, null, null,
                null, 0, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Oculus API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="oculusDevice">the device your authenticating on</param>
        /// <param name="nonce">the nonce</param>
        /// <param name="oculusToken">the user's oculus token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <param name="userId"></param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnityAsync.AuthenticateUserViaOculus"/>
        /// <example><code>
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
        /// </code></example>
        public static void AuthenticateUserViaOculus(OculusDevice oculusDevice, string nonce,
                                                     long userId, string oculusToken,
                                                     string emailAddress,
                                                     TermsHash? hash,
                                                     Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                oculusToken, AuthenticationServiceProvider.Oculus, emailAddress, hash, nonce,
                oculusDevice, userId.ToString(), 0, callback);
        }

        /// <summary>
        /// Attempts to authenticate a user on behalf of an OpenID identity provider. To use this
        /// method of authentication, you must configure the OpenID config in your games
        /// authentication admin page.
        /// NOTE: The ability to authenticate players using your identity provider is a feature for
        /// advanced partners only. If you are interested in becoming an advanced partner, please
        /// contact us.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="idToken">the user's id token</param>
        /// <param name="emailAddress">the user's email address</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="callback">Callback to be invoked when the operation completes</param>
        /// <seealso cref="GetTermsOfUse"/>
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
        ///     ModIOUnity.AuthenticateUserViaOpenId(idToken,
        ///                                          "johndoe@gmail.com",
        ///                                          modIOTermsOfUse.hash,
        ///                                          AuthenticationCallback);
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
        public static void AuthenticateUserViaOpenId(string idToken,
                                                     string emailAddress,
                                                     TermsHash? hash,
                                                     Action<Result> callback)
        {
            ModIOUnityImplementation.AuthenticateUser(
                idToken, AuthenticationServiceProvider.OpenId, emailAddress, hash, null,
                null, null, 0, callback);
        }

        /// <summary>
        /// Informs you if the current user session is authenticated or not.
        /// </summary>
        /// <param name="callback"></param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnityAsync.IsAuthenticated"/>
        /// <example><code>
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
        /// </code></example>
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
        /// <example><code>
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
        /// </code></example>
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
        /// <example><code>
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
        /// </code></example>
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
        /// <example><code>
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
        ///         Debug.Log("ModPage has " + modPage.modProfiles.Length + " mods");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get mods");
        ///     }
        /// }
        /// </code></example>
        public static void GetMods(SearchFilter filter, Action<ResultAnd<ModPage>> callback)
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
        /// <example><code>
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
        /// </code></example>
        public static void GetMod(ModId modId, Action<ResultAnd<ModProfile>> callback)
        {
            ModIOUnityImplementation.GetMod(modId.id, callback);
        }

        public static void GetModSkipCache(ModId modId, Action<ResultAnd<ModProfile>> callback) => ModIOUnityImplementation.GetModSkipCache(modId.id, callback);

        /// <summary>
        /// Get all comments posted in the mods profile. Successful request will return an array of
        /// Comment Objects. We recommended reading the filtering documentation to return only the
        /// records you want.
        /// </summary>
        ///  <param name="filter">The filter to apply when searching through comments (can only apply
        /// pagination parameters, Eg. page size and page index)</param>
        /// <param name="callback">callback invoked with the Result and CommentPage</param>
        /// <seealso cref="CommentPage"/>
        /// <seealso cref="ModComment"/>
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ResultAnd"/>
        /// <seealso cref="ModIOUnityAsync.GetModComments"/>
        public static void GetModComments(ModId modId, SearchFilter filter, Action<ResultAnd<CommentPage>> callback)
        {
            ModIOUnityImplementation.GetModComments(modId, filter, callback);
        }

        /// <summary>
        /// Retrieves a list of ModDependenciesObjects that represent mods that depend on a mod.
        /// </summary>
        /// <remarks>
        /// This function returns only immediate mod dependencies, meaning that if you need the dependencies for the dependent
        /// mods, you will have to make multiple calls and watch for circular dependencies.
        /// </remarks>
        /// <param name="modId">the ModId of the mod to get dependencies</param>
        /// <param name="callback">callback with the Result and an array of ModDependenciesObjects</param>
        /// <seealso cref="ModId"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ResultAnd"/>
        /// <seealso cref="ModDependenciesObject"/>
        /// <seealso cref="ModIOUnityAsync.GetModDependencies"/>
        /// <example><code>
        /// void Example()
        /// {
        ///     ModId modId = new ModId(1234);
        ///     ModIOUnity.GetModDependencies(modId, GetModCallback);
        /// }
        ///
        /// void GetModCallback(ResultAnd&lt;ModDependenciesObject[]&gt; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         ModDependenciesObject[] modDependenciesObjects = response.value;
        ///         Debug.Log("retrieved mods dependencies");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get mod dependencies");
        ///     }
        /// }
        /// </code></example>
        public static void GetModDependencies(ModId modId, Action<ResultAnd<ModDependencies[]>> callback)
        {
            ModIOUnityImplementation.GetModDependencies(modId, callback);
        }

        /// <summary>
        /// Get all mod rating's submitted by the authenticated user. Successful request will return an array of Rating Objects.
        /// </summary>
        /// <param name="callback">callback with the Result and an array of RatingObject</param>
        /// <seealso cref="ModId"/>
        /// <seealso cref="RatingObject"/>
        /// <seealso cref="ResultAnd"/>
        /// <example><code>
        /// void Example()
        /// {
        ///    ModIOUnity.GetCurrentUserRatings(GetCurrentUserRatingsCallback);
        /// }
        ///
        /// void GetCurrentUserRatingsCallback(ResultAnd&lt;Rating[]&gt; response)
        /// {
        ///    if (response.result.Succeeded())
        ///    {
        ///        foreach(var ratingObject in response.value)
        ///        {
        ///            Debug.Log($"retrieved rating '{ratingObject.rating}' for {ratingObject.modId}");
        ///        }
        ///    }
        ///    else
        ///    {
        ///        Debug.Log("failed to get ratings");
        ///    }
        /// }
        /// </code></example>
        public static void GetCurrentUserRatings(Action<ResultAnd<Rating[]>> callback)
        {
            ModIOUnityImplementation.GetCurrentUserRatings(callback);
        }

        /// <summary>
        /// Gets the rating that the current user has given for a specified mod. You must have an
        /// authenticated session for this to be successful.
        /// </summary>
        /// <remarks>Note that the rating can be 'None'</remarks>
        /// <param name="modId">the id of the mod to check for a rating</param>
        /// <param name="callback">callback with the result and rating of the specified mod</param>
        /// <seealso cref="ModRating"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ResultAnd"/>
        /// <example><code>
        /// void Example()
        /// {
        ///    ModId modId = new ModId(1234);
        ///    ModIOUnity.GetCurrentUserRatingFor(modId, GetRatingCallback);
        /// }
        ///
        /// void GetRatingCallback(ResultAnd&lt;ModRating&gt; response)
        /// {
        ///    if (response.result.Succeeded())
        ///    {
        ///        Debug.Log($"retrieved rating: {response.value}");
        ///    }
        ///    else
        ///    {
        ///        Debug.Log("failed to get rating");
        ///    }
        /// }
        /// </code></example>
        public static void GetCurrentUserRatingFor(ModId modId, Action<ResultAnd<ModRating>> callback)
        {
            ModIOUnityImplementation.GetCurrentUserRatingFor(modId, callback);
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
        /// <example><code>
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
        ///     }
        /// }
        /// </code></example>
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
        /// <example><code>
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
        ///     }
        /// }
        /// </code></example>
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
        /// <example><code>
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
        ///     }
        /// }
        /// </code></example>
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
        /// <example><code>
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
        /// </code></example>
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
        /// <example><code>
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
        /// </code></example>
        public static void GetCurrentUser(Action<ResultAnd<UserProfile>> callback)
        {
            ModIOUnityImplementation.GetCurrentUser(callback);
        }

        /// <summary>
        /// Mutes a user which effectively hides any content from that specified user
        /// </summary>
        /// <remarks>The userId can be found from the UserProfile. Such as ModProfile.creator.userId</remarks>
        /// <param name="userId">The id of the user to be muted</param>
        /// <param name="callback">callback with the Result of the request</param>
        /// <seealso cref="UserProfile"/>
        public static void MuteUser(long userId, Action<Result> callback)
        {
            ModIOUnityImplementation.MuteUser(userId, callback);
        }

        /// <summary>
        /// Un-mutes a user which effectively reveals previously hidden content from that user
        /// </summary>
        /// <remarks>The userId can be found from the UserProfile. Such as ModProfile.creator.userId</remarks>
        /// <param name="userId">The id of the user to be muted</param>
        /// <param name="callback">callback with the Result of the request</param>
        /// <seealso cref="UserProfile"/>
        public static void UnmuteUser(long userId, Action<Result> callback)
        {
            ModIOUnityImplementation.UnmuteUser(userId, callback);
        }

        /// <summary>
        /// Gets an array of all the muted users that the current authenticated user has muted.
        /// </summary>
        /// <remarks>This has a cap of 1,000 users. It will not return more then that.</remarks>
        /// <param name="callback">callback with the Result of the request</param>
        /// <seealso cref="UserProfile"/>
        public static void GetMutedUsers(Action<ResultAnd<UserProfile[]>> callback)
        {
            ModIOUnityImplementation.GetMutedUsers(callback);
        }

        #endregion

        #region Mod Management

        /// <summary>
        /// This retrieves the user's ratings and subscriptions from the mod.io server and synchronises
        /// it with our local instance of the user's data. If mod management has been enabled
        /// via ModIOUnity.EnableModManagement() then it may begin to install/uninstall mods.
        /// It's recommended you use this method after initializing the plugin and after
        /// successfully authenticating the user.
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
        /// <example><code>
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
        /// </code></example>
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
        /// <example><code>
        /// void Example()
        /// {
        ///     Result result = ModIOUnity.EnableModManagement(ModManagementDelegate);
        /// }
        ///
        /// void ModManagementDelegate(ModManagementEventType eventType, ModId modId)
        /// {
        ///     Debug.Log("a mod management event of type " + eventType.ToString() + " has been invoked");
        /// }
        /// </code></example>
        public static Result EnableModManagement(
            ModManagementEventDelegate modManagementEventDelegate)
        {
            return ModIOUnityImplementation.EnableModManagement(modManagementEventDelegate);
        }

        /// <summary>
        /// Disables the mod management system and cancels any ongoing jobs for downloading or
        /// installing mods.
        /// </summary>
        /// <example><code>
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
        /// </code></example>
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
        /// <example><code>
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
        /// </code></example>
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
        /// <example><code>
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
        /// </code></example>
        public static InstalledMod[] GetSystemInstalledMods(out Result result)
        {
            return ModIOUnityImplementation.GetInstalledMods(out result);
        }

        /// <summary>
        /// Gets an array of mods that are installed for the current user.
        /// </summary>
        /// <param name="result">an out Result to inform whether or not it was able to get installed mods</param>
        /// <param name="includeDisabledMods">optional parameter. When true it will include mods that have been marked as disabled via the <see cref="DisableMod"/> method</param>
        /// <seealso cref="UserInstalledMod"/>
        /// <seealso cref="GetSubscribedMods"/>
        /// <seealso cref="ModIOUnity.DisableMod"/>
        /// <seealso cref="ModIOUnity.EnableMod"/>
        /// <returns>an array of InstalledModUser for each existing mod installed for the user</returns>
        /// <example><code>
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
        /// </code></example>
        public static UserInstalledMod[] GetInstalledModsForUser(out Result result, bool includeDisabledMods = false)
        {
            return ModIOUnityImplementation.GetInstalledModsForUser(out result, includeDisabledMods);
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
        /// <example><code>
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
        /// </code></example>
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
        /// <example><code>
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
        /// </code></example>
        public static bool IsModManagementBusy()
        {
            return ModIOUnityImplementation.IsModManagementBusy();
        }

        public static bool EnableMod(ModId modId)
        {
            return ModIOUnityImplementation.EnableMod(modId);
        }

        public static bool DisableMod(ModId modId)
        {
            return ModIOUnityImplementation.DisableMod(modId);
        }

        /// <summary>
        /// Adds the specified mods as dependencies to an existing mod.
        /// </summary>
        /// <remarks>
        /// If the dependencies already exist they will be ignored and the result will return success
        /// </remarks>
        /// <param name="modId">ModId of the mod you want to add dependencies to</param>
        /// <param name="dependencies">The ModIds that you want to add (max 5 at a time)</param>
        /// <param name="callback">callback with the result of the request</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModIOUnity.RemoveDependenciesFromMod"/>
        /// <seealso cref="ModIOUnityAsync.RemoveDependenciesFromMod"/>
        /// <seealso cref="ModIOUnityAsync.AddDependenciesToMod"/>
        /// <example><code>
        /// void Example()
        /// {
        ///     var dependencies = new List&#60;ModId&#62;
        ///     {
        ///         (ModId)1001,
        ///         (ModId)1002,
        ///         (ModId)1003
        ///     };
        ///     ModIOUnity.AddDependenciesToMod(mod.id, dependencies, AddDependenciesCallback);
        /// }
        ///
        /// void AddDependenciesCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully added dependencies to mod");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to add dependencies to mod");
        ///     }
        /// }
        /// </code></example>
        public static void AddDependenciesToMod(ModId modId, ICollection<ModId> dependencies, Action<Result> callback)
        {
            ModIOUnityImplementation.AddDependenciesToMod(modId, dependencies, callback);
        }

        /// <summary>
        /// Removes the specified mods as dependencies for another existing mod.
        /// </summary>
        /// <remarks>
        /// If the dependencies dont exist they will be ignored and the result will return success
        /// </remarks>
        /// <param name="modId">ModId of the mod you want to remove dependencies from</param>
        /// <param name="dependencies">The ModIds that you want to remove (max 5 at a time)</param>
        /// <param name="callback">callback with the result of the request</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="dependencies"/>
        /// <seealso cref="ModIOUnity.AddDependenciesToMod"/>
        /// <seealso cref="ModIOUnityAsync.RemoveDependenciesFromMod"/>
        /// <seealso cref="ModIOUnityAsync.AddDependenciesToMod"/>
        /// <example><code>
        /// void Example()
        /// {
        ///     var dependencies = new List&#60;ModId&#62;
        ///     {
        ///         (ModId)1001,
        ///         (ModId)1002,
        ///         (ModId)1003
        ///     };
        ///     ModIOUnity.RemoveDependenciesFromMod(mod.id, dependencies, RemoveDependenciesCallback);
        /// }
        ///
        /// void RemoveDependenciesCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully removed dependencies from mod");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to removed dependencies from mod");
        ///     }
        /// }
        /// </code></example>
        public static void RemoveDependenciesFromMod(ModId modId, ICollection<ModId> dependencies, Action<Result> callback)
        {
            ModIOUnityImplementation.RemoveDependenciesFromMod(modId, dependencies, callback);
        }

        /// <summary>
        /// Stops any current download and starts downloading the selected mod.
        /// </summary>
        /// <param name="modId">ModId of the mod you want to remove dependencies from</param>
        /// <param name="callback">callback with the result of the request</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModIOUnityAsync.DownloadNow"/>
        /// <example><code>
        /// ModId modId;
        /// void Example()
        /// {
        ///     ModIOUnity.DownloadNow(modId, callback);
        /// }
        ///
        /// void RemoveDependenciesCallback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Successful");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed");
        ///     }
        /// }
        /// </code></example>
        public static void DownloadNow(ModId modId, Action<Result> callback)
        {
            ModIOUnityImplementation.DownloadNow(modId, callback);
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
        /// <example><code>
        /// void Example()
        /// {
        ///     CreationToken token = ModIOUnity.GenerateCreationToken();
        /// }
        /// </code></example>
        public static CreationToken GenerateCreationToken()
        {
            return ModIOUnityImplementation.GenerateCreationToken();
        }

        /// <summary>
        /// Creates a new mod profile on the mod.io server based on the details provided from the
        /// ModProfileDetails object provided. Note that you must have a logo, name and summary
        /// assigned in ModProfileDetails in order for this to work.
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
        /// <example><code>
        /// ModId newMod;
        /// Texture2D logo;
        /// CreationToken token;
        ///
        /// void Example()
        /// {
        ///     token = ModIOUnity.GenerateCreationToken();
        ///
        ///     ModProfileDetails profile = new ModProfileDetails();
        ///     profile.name = "mod name";
        ///     profile.summary = "a brief summary about this mod being submitted"
        ///     profile.logo = logo;
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
        /// </code></example>
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
        /// <example><code>
        /// ModId modId;
        ///
        /// void Example()
        /// {
        ///     ModProfileDetails profile = new ModProfileDetails();
        ///     profile.modId = modId;
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
        /// </code></example>
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
        /// <seealso cref="AddModfile"/>
        /// <seealso cref="ArchiveModProfile"/>
        /// <example><code>
        /// void Example()
        /// {
        ///     ProgressHandle handle = ModIOUnity.GetCurrentUploadHandle();
        ///
        ///     if (handle != null)
        ///     {
        ///         Debug.Log("Current upload progress is: " + handle.Progress.ToString());
        ///     }
        /// }
        /// </code></example>
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
        /// <seealso cref="ModIOUnityAsync.AddModfile"/>
        /// <seealso cref="UploadModMedia"/>
        /// <example><code>
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
        /// </code></example>
        public static void UploadModfile(ModfileDetails modfile, Action<Result> callback)
        {
            ModIOUnityImplementation.AddModfile(modfile, callback);
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
        /// <example><code>
        /// ModId modId;
        /// Texture2D newTexture;
        ///
        /// void Example()
        /// {
        ///     ModProfileDetails profile = new ModProfileDetails();
        ///     profile.modId = modId;
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
        /// </code></example>
        public static void UploadModMedia(ModProfileDetails modProfileDetails, Action<Result> callback)
        {
            ModIOUnityImplementation.UploadModMedia(modProfileDetails, callback);
        }

        /// <summary>
        /// <p>Reorder a mod's gallery images. <paramref name="orderedFilenames"/> must represent every entry in <see cref="ModProfile.galleryImages_Original"/> (or any of the size-variant arrays) or the operation will fail.</p>
        /// <p>The provided <paramref name="callback"/> is invoked with the updated <see cref="ModProfile"/>.</p>
        /// </summary>
        public static void ReorderModMedia(ModId modId, string[] orderedFilenames, Action<ResultAnd<ModProfile>> callback)
        {
            ModIOUnityImplementation.ReorderModMedia(modId, orderedFilenames, callback);
        }

        /// <summary>
        /// <p>Delete gallery images from a mod. Filenames can be sourced from <see cref="ModProfile.galleryImages_Original"/> (or any of the size-variant arrays).</p>
        /// <p>The provided <paramref name="callback"/> is invoked with the updated <see cref="ModProfile"/>.</p>
        /// </summary>
        public static void DeleteModMedia(ModId modId, string[] filenames, Action<ResultAnd<ModProfile>> callback)
        {
            ModIOUnityImplementation.DeleteModMedia(modId, filenames, callback);
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
        /// <example><code>
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
        /// </code></example>
        public static void ArchiveModProfile(ModId modId, Action<Result> callback)
        {
            ModIOUnityImplementation.ArchiveModProfile(modId, callback);
        }

        /// <summary>
        /// Get all mods the authenticated user added or is a team member of.
        /// Successful request will return an array of Mod Objects. We
        /// recommended reading the filtering documentation to return only
        /// the records you want.
        /// </summary>
        public static void GetCurrentUserCreations(SearchFilter filter, Action<ResultAnd<ModPage>> callback)
        {
            ModIOUnityImplementation.GetCurrentUserCreations(filter, callback);
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
        /// <example><code>
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
        /// </code></example>
        public static void AddTags(ModId modId, string[] tags, Action<Result> callback)
        {
            ModIOUnityImplementation.AddTags(modId, tags, callback);
        }

        /// <summary>
        /// Adds a comment to a mod profile. Successfully adding a comment returns the Mod Comment
        /// object back.
        /// </summary>
        /// <remarks>Keep in mind you can use mentions in the comment content, such as "Hello there, @&lt;john-doe&gt;"</remarks>
        /// <param name="modId">Id of the mod to add the comment to</param>
        /// <param name="commentDetails">the new comment to be added</param>
        /// <param name="callback">callback with the result of the operation</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ResultAnd"/>
        /// <seealso cref="ModComment"/>
        /// <seealso cref="CommentDetails"/>
        /// <seealso cref="GetModComments"/>
        /// <seealso cref="DeleteModComment"/>
        /// <seealso cref="EditModComment"/>
        /// <seealso cref="ModIOUnityAsync.AddModComment"/>
        /// <example><code>
        /// ModId modId;
        ///
        /// void Example()
        /// {
        ///     CommentDetails comment = new CommentDetails(0, "Hello world!");
        ///     ModIOUnity.AddModComment(modId, comment, AddCommentCallback);
        /// }
        ///
        /// void AddCommentCallback(ResultAnd&lt;ModComment&gt; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("added comment");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to add comment");
        ///     }
        /// }
        /// </code></example>
        public static void AddModComment(ModId modId, CommentDetails commentDetails, Action<ResultAnd<ModComment>> callback)
        {
            ModIOUnityImplementation.AddModComment(modId, commentDetails, callback);
        }

        /// <summary>
        /// Delete a comment from a mod profile. Successful request will return 204 No Content and fire a MOD_COMMENT_DELETED event.
        /// </summary>
        /// <param name="modId">Id of the mod to add the comment to</param>
        /// <param name="commentId">The id for the comment to be removed</param>
        /// <param name="callback">callback with the result of the operation</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModComment"/>
        /// <seealso cref="CommentDetails"/>
        /// <seealso cref="DeleteModComment"/>
        /// <seealso cref="EditModComment"/>
        /// <seealso cref="ModIOUnityAsync.DeleteModComment"/>
        /// <example><code>
        ///private ModId modId;
        ///private long commentId;
        ///
        ///void Example()
        ///{
        ///    ModIOUnity.DeleteModComment(modId, commentId, DeleteCommentCallback);
        ///}
        ///
        ///void DeleteCommentCallback(Result result)
        ///{
        ///    if (result.Succeeded())
        ///    {
        ///         Debug.Log("deleted comment");
        ///     }
        ///     else
        ///    {
        ///         Debug.Log("failed to delete comment");
        ///     }
        /// }
        /// </code></example>
        public static void DeleteModComment(ModId modId, long commentId, Action<Result> callback)
        {
            ModIOUnityImplementation.DeleteModComment(modId, commentId, callback);
        }

        /// <summary>
        /// Update a comment for the corresponding mod. Successful request will return the updated Comment Object.
        /// </summary>
        /// <param name="modId">Id of the mod the comment is on</param>
        /// <param name="content">Updated contents of the comment.</param>
        /// <param name="commentId">The id for the comment you wish to edit</param>
        /// <param name="callback">callback with the result of the operation</param>
        /// <seealso cref="ResultAnd"/>
        /// <seealso cref="ModComment"/>
        /// <seealso cref="ModIOUnityAsync.UpdateModComment"/>
        /// <example><code>
        /// private string content = "This is a Comment";
        /// long commentId = 12345;
        /// ModId modId = (ModId)1234;
        ///
        /// void UpdateMod()
        /// {
        ///     ModIOUnity.UpdateModComment(modId, content, commentId, UpdateCallback);
        /// }
        ///
        /// void UpdateCallback(ResultAnd&#60;ModComment&#62; resultAnd)
        /// {
        ///     if(resultAnd.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully Updated Comment!");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to Update Comment!");
        ///     }
        /// }
        /// </code></example>
        public static void UpdateModComment(ModId modId, string content, long commentId, Action<ResultAnd<ModComment>> callback)
        {
            ModIOUnityImplementation.UpdateModComment(modId, content, commentId, callback);
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
        /// <example><code>
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
        /// </code></example>
        public static void DeleteTags(ModId modId, string[] tags, Action<Result> callback)
        {
            ModIOUnityImplementation.DeleteTags(modId, tags, callback);
        }

#endregion // Mod Uploading

        #region Multipart
        /// <summary>
    /// Get all upload sessions belonging to the authenticated user for the corresponding mod. Successful request will return an
    /// array of Multipart Upload Part Objects. We recommended reading the filtering documentation to return only the records you want.
    /// The Mutlipart feature is automatically used when uploading a mod via UploadModFile and is limited to one upload at a time.
    /// This function is optional and is provided to allow for more control over uploading large files for those who require it.
    /// </summary>
    /// <param name="modId">the id of the mod</param>
    /// <param name="filter">The filter to apply when searching through comments (can only apply
    /// pagination parameters, Eg. page size and page index)</param>
    /// <param name="callback">a callback with the Result of the operation</param>
    /// <seealso cref="SearchFilter"/>
    /// <seealso cref="ModIOUnityAsync.UploadModfile"/>
    /// <seealso cref="ModIOUnityAsync.GetMultipartUploadSessions"/>
    /// <example><code>
    /// ModId modId;
    /// SearchFilter filter;
    ///
    /// private void Example()
    /// {
    ///     ModIOUnity.GetMultipartUploadSessions(modId, filter, Callback);
    /// }
    /// void Callback(ResultAnd&#60;MultipartUploadSessionsObject&#62; response)
    /// {
    ///     if (response.result.Succeeded())
    ///     {
    ///         Debug.Log("Received Upload Sessions");
    ///     }
    ///     else
    ///     {
    ///         Debug.Log("Failed to get Upload Sessions");
    ///     }
    /// }
    /// </code></example>
        public static void GetMultipartUploadSessions(ModId modId, SearchFilter filter, Action<ResultAnd<PaginatedResponse<MultipartUpload>>> callback)
    {
        ModIOUnityImplementation.GetMultipartUploadSessions(modId, filter, callback);
    }

        /// <summary>
        /// Get all uploaded parts for a corresponding upload session. Successful request will return an array of Multipart
        /// Upload Part Objects.We recommended reading the filtering documentation to return only the records you want.
        /// The Mutlipart feature is automatically used when uploading a mod via UploadModFile and is limited to one upload at a time.
        /// This function is optional and is provided to allow for more control over uploading large files for those who require it.
        /// </summary>
        /// <param name="modId">the id of the mod</param>
        /// <param name="uploadId">A universally unique identifier (UUID) that represents the upload session.</param>
        /// <param name="filter">The filter to apply when searching through comments (can only apply
        /// pagination parameters, Eg. page size and page index)</param>
        /// <param name="callback">a callback with the Result of the operation</param>
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="ModIOUnityAsync.UploadModfile"/>
        /// <seealso cref="ModIOUnityAsync.GetMultipartUploadParts"/>
        /// <example><code>
        /// ModId modId;
        /// string uploadId;
        /// SearchFilter filter;
        ///
        /// private void Example()
        /// {
        ///     ModIOUnity.GetMultipartUploadParts(modId, uploadId, filter, Callback);
        /// }
        /// void Callback(ResultAnd&#60;MultipartUploadSessionsObject&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Received Upload Sessions Object");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to get Upload Sessions Object");
        ///     }
        /// }
        /// </code></example>
        public static void GetMultipartUploadParts(ModId modId, string uploadId, SearchFilter filter, Action<ResultAnd<PaginatedResponse<MultipartUploadPart>>> callback)
        {
            ModIOUnityImplementation.GetMultipartUploadParts(modId, uploadId, filter, callback);
        }

        /// <summary>
        /// Add a new multipart upload part to an existing upload session. All parts must be exactly 50MB (Mebibyte) in size unless it is the
        /// final part which can be smaller. A successful request will return a single Multipart Upload Part Object.
        /// NOTE: Unlike other POST endpoints on this service, the body of this request should contain no form parameters and instead be the data
        /// described in the byte range of the Content-Range header of the request.
        /// The Mutlipart feature is automatically used when uploading a mod via UploadModFile and is limited to one upload at a time.
        /// This function is optional and is provided to allow for more control over uploading large files for those who require it.
        /// </summary>
        /// <param name="modId">the id of the mod</param>
        /// <param name="uploadId">A universally unique identifier (UUID) that represents the upload session.</param>
        /// <param name="contentRange">The Content-Range of the file you are sending.
        /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Range</param>
        /// <param name="digest">Optional Digest for part integrity checks once the part has been uploaded.</param>
        /// <param name="rawBytes">Bytes for the file part to be uploaded</param>
        /// <param name="callback">a callback with the Result of the operation</param>
        /// <seealso cref="ModIOUnityAsync.UploadModfile"/>
        /// <seealso cref="ModIOUnityAsync.AddMultipartUploadParts"/>
        /// <example><code>
        /// ModId modId;
        /// string uploadId;
        /// string contentRange;
        /// string digest;
        /// byte[] rawBytes;
        ///
        /// private void Example()
        /// {
        ///     ModIOUnity.AddMultipartUploadParts(modId, uploadId, contentRange, digest, rawBytes, Callback);
        /// }
        /// void Callback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Added a part to Upload Session");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to add a part to Upload Session");
        ///     }
        /// }
        /// </code></example>
        public static void AddMultipartUploadParts(ModId modId, string uploadId, string contentRange, string digest, byte[] rawBytes, Action<Result> callback)
        {
            ModIOUnityImplementation.AddMultipartUploadParts(modId, uploadId, contentRange, digest, rawBytes, callback);
        }

        /// <summary>
        /// Create a new multipart upload session. A successful request will return a single Multipart Upload Object.
        /// NOTE: The multipart upload system is designed for uploading large files up to 20GB in size. If uploading
        /// files less than 100MB, we recommend using the Add Modfile endpoint.
        /// The Mutlipart feature is automatically used when uploading a mod via UploadModFile and is limited to one upload at a time.
        /// This function is optional and is provided to allow for more control over uploading large files for those who require it.
        /// </summary>
        /// <param name="modId">the id of the mod</param>
        /// <param name="nonce">An optional nonce to provide to prevent duplicate upload sessions from being created concurrently. Maximum of 64 characters.</param>
        /// <param name="filename">The filename of the file once all the parts have been uploaded. The filename must include the .zip extension and cannot exceed 100 characters.</param>
        /// <param name="callback">a callback with the Result of the operation</param>
        /// <seealso cref="ModIOUnityAsync.UploadModfile"/>
        /// <seealso cref="ModIOUnityAsync.CreateMultipartUploadSession"/>
        /// <example><code>
        /// ModId modId;
        /// string filename;
        /// string nonce;
        ///
        /// private void Example()
        /// {
        ///     ModIOUnity.CreateMultipartUploadSession(modId, filename, nonce, Callback);
        /// }
        /// void Callback(ResultAnd&#60;MultipartUploadObject&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Created Upload Session");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to Create Upload Session");
        ///     }
        /// }
        /// </code></example>
        public static void CreateMultipartUploadSession(ModId modId, string filename, string nonce = null, Action<ResultAnd<MultipartUpload>> callback = null)
        {
            ModIOUnityImplementation.CreateMultipartUploadSession(modId, filename, nonce, callback);
        }

        /// <summary>
        /// Terminate an active multipart upload session, a successful request will return 204 No Content.
        /// The Mutlipart feature is automatically used when uploading a mod via UploadModFile and is limited to one upload at a time.
        /// This function is optional and is provided to allow for more control over uploading large files for those who require it.
        /// </summary>
        /// <param name="modId">the id of the mod</param>
        /// <param name="uploadId">A universally unique identifier (UUID) that represents the upload session.</param>
        /// <param name="callback">a callback with the Result of the operation</param>
        /// <seealso cref="ModIOUnityAsync.DeleteMultipartUploadSession"/>
        /// <seealso cref="ModIOUnityAsync.UploadModfile"/>
        /// <example><code>
        /// ModId modId;
        /// string uploadId;
        ///
        /// private void Example()
        /// {
        ///     ModIOUnity.DeleteMultipartUploadSession(modId, uploadId, Callback);
        /// }
        /// void Callback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Deleted Upload Session");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to Delete Upload Session");
        ///     }
        /// }
        /// </code></example>
        public static void DeleteMultipartUploadSession(ModId modId, string uploadId, Action<Result> callback)
        {
            ModIOUnityImplementation.DeleteMultipartUploadSession(modId, uploadId, callback);
        }

        /// <summary>
        /// Complete an active multipart upload session, this endpoint assumes that you have already uploaded all individual parts.
        /// A successful request will return a 200 OK response code and return a single Multipart Upload Object.
        /// The Mutlipart feature is automatically used when uploading a mod via UploadModFile and is limited to one upload at a time.
        /// This function is optional and is provided to allow for more control over uploading large files for those who require it.
        /// </summary>
        /// <param name="modId">the id of the mod</param>
        /// <param name="uploadId">A universally unique identifier (UUID) that represents the upload session.</param>
        /// <param name="callback">a callback with the Result of the operation</param>
        /// <seealso cref="ModIOUnityAsync.CompleteMultipartUploadSession"/>
        /// <seealso cref="ModIOUnityAsync.UploadModfile"/>
        /// <example><code>
        /// ModId modId;
        /// string uploadId;
        ///
        /// private void Example()
        /// {
        ///     ModIOUnity.CompleteMultipartUploadSession(modId, uploadId, Callback);
        /// }
        /// void Callback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Completed Session");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to complete session");
        ///     }
        /// }
        /// </code></example>
        public static void CompleteMultipartUploadSession(ModId modId, string uploadId, Action<Result> callback)
        {
            ModIOUnityImplementation.CompleteMultipartUploadSession(modId, uploadId, callback);
        }

        #endregion

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
        /// <example><code>
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
        /// </code></example>
#if UNITY_2019_4_OR_NEWER
        public static void DownloadTexture(DownloadReference downloadReference,
                                           Action<ResultAnd<Texture2D>> callback)
        {
            ModIOUnityImplementation.DownloadTexture(downloadReference, callback);
        }
#endif
        public static void DownloadImage(DownloadReference downloadReference,
                                           Action<ResultAnd<byte[]>> callback)
        {
            ModIOUnityImplementation.DownloadImage(downloadReference, callback);
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
        /// <example><code>
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
        /// </code></example>
        public static void Report(Report report, Action<Result> callback)
        {
            ModIOUnityImplementation.Report(report, callback);
        }

        #endregion // Reporting

        #region Monetization

        public static void GetTokenPacks(Action<ResultAnd<TokenPack[]>> callback) => ModIOUnityImplementation.GetTokenPacks(callback);

        /// <summary>
        /// Convert an in-game consumable that a user has purchased on Steam, Xbox, or Psn into a users
        /// mod.io inventory. This endpoint will consume the entitlement on behalf of the user against
        /// the portal in which the entitlement resides (i.e. Steam, Xbox, Psn).
        /// </summary>
        /// <param name="callback">a callback with the Result of the operation</param>
        /// <seealso cref="Entitlement"/>
        /// <seealso cref="EntitlementObject"/>
        /// <seealso cref="ModIOUnityAsync.SyncEntitlements"/>
        /// <code>
        ///
        /// private void Example(string token)
        /// {
        ///     ModIOUnity.SyncEntitlements(token);
        /// }
        /// void Callback(ResultAnd&#60;Entitlement[]&#62; response)
        /// {
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("Sync Entitlements Success");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to Sync Entitlements");
        ///     }
        /// }
        /// </code>
        public static void SyncEntitlements(Action<ResultAnd<Entitlement[]>> callback)
        {
            ModIOUnityImplementation.SyncEntitlements(callback);
        }

        /// <summary>
        /// Complete a marketplace purchase. A Successful request will return the newly created Checkout
        /// Process Object. Parameter|Type|Required|Description ---|---|---|---| transaction_id|integer|true|The id
        /// of the transaction to complete. mod_id|integer|true|The id of the mod associated to this transaction.
        /// display_amount|integer|true|The expected amount of the transaction to confirm the displayed amount matches
        /// the actual amount.
        /// </summary>
        /// <param name="modId">The id of the mod the user wants to purchase.</param>
        /// <param name="displayAmount">The amount that was shown to the user for the purchase.</param>
        /// <param name="idempotent">A unique string. Must be alphanumeric and cannot contain unique characters except for - </param>
        /// <param name="callback">callback with the result of the operation</param>
        /// <seealso cref="Result"/>
        /// <code>
        ///
        /// string idempotent = $"aUniqueKey";
        /// ModId modId = 1234;
        /// int displayAmount = 12;
        ///
        /// void Example()
        /// {
        ///     ModIOUnity.PurchaseMod(modId, displayAmount, idempotent, Callback);
        /// }
        ///
        /// void Callback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Completed Purchase");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to complete purchase");
        ///     }
        /// }
        /// </code>
        public static void PurchaseMod(ModId modId, int displayAmount, string idempotent, Action<ResultAnd<CheckoutProcess>> callback)
        {
            ModIOUnityImplementation.PurchaseMod(modId, displayAmount, idempotent, callback);
        }

        /// <summary>
        /// Retrieves all of the purchased mods for the current user.
        /// </summary>
        /// <param name="result">an out parameter for whether or not the method succeeded</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="FetchUpdates"/>
        /// <returns>an array of the user's purchased mods</returns>
        /// <code>
        /// void Example()
        /// {
        ///     ModProfile[] mods = ModIOUnity.GetPurchasedMods(out Result result);
        ///
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("user has " + mods.Length + " purchased mods");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get purchased mods");
        ///     }
        /// }
        /// </code>
        public static ModProfile[] GetPurchasedMods(out Result result)
        {
            return ModIOUnityImplementation.GetPurchasedMods(out result);
        }

        /// <summary>
        /// Get user's wallet balance
        /// </summary>
        /// <param name="callback">callback with the result of the operation</param>
        /// <seealso cref="Result"/>
        /// <code>
        ///
        /// void Example()
        /// {
        ///     ModIOUnity.GetUserWalletBalance(filter, Callback);
        /// }
        ///
        /// void Callback(Result result)
        /// {
        ///     if (result.Succeeded())
        ///     {
        ///         Debug.Log("Get balance Success");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get balance");
        ///     }
        /// }
        /// </code>
        public static void GetUserWalletBalance(Action<ResultAnd<Wallet>> callback)
        {
            ModIOUnityImplementation.GetUserWalletBalance(callback);
        }

        /// <summary>
        /// Get all <see cref="MonetizationTeamAccount"/> for a specific mod
        /// </summary>
        /// <param name="modId">The mod to get users for</param>
        /// <param name="callback">callback with the result of the operation</param>
        public static void GetModMonetizationTeam(ModId modId, Action<ResultAnd<MonetizationTeamAccount[]>> callback)
        {
            ModIOUnityImplementation.GetModMonetizationTeam(callback, modId);
        }


        /// <summary>
        /// Set all <see cref="ModMonetizationTeamDetails"/> for a specific mod
        /// </summary>
        /// <param name="modId">The mod to set users for</param>
        /// <param name="team">All users and their splits</param>
        /// <param name="callback">callback with the result of the operation</param>
        public static void AddModMonetizationTeam(ModId modId, ICollection<ModMonetizationTeamDetails> team, Action<Result> callback)
        {
            ModIOUnityImplementation.AddModMonetizationTeam(callback, modId, team);
        }
        #endregion

    }
}

#pragma warning restore 4014 // Restore warnings about calling async functions from non-async code
