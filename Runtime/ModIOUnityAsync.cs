using ModIO.Implementation;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.API.Requests;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable 4014 // Ignore warnings about calling async functions from non-async code

namespace ModIO
{
    /// <summary>Main async interface for the mod.io Unity plugin.</summary>
    /// <remarks>Every <see cref="ModIOUnityAsync"/> method has a callback alternative in <see cref="ModIOUnity"/>.</remarks>
    /// <seealso cref="ModIOUnity"/>
    public static class ModIOUnityAsync
    {
        #region Initialization and Maintenance

        /// <summary>Cancels all public operations, frees plugin resources and invokes any pending callbacks with a cancelled result code.</summary>
        /// <remarks><c>Result.IsCancelled()</c> can be used to determine if it was cancelled due to a shutdown operation.</remarks>
        /// <example><code>
        /// await ModIOUnityAsync.Shutdown();
        /// Debug.Log("Plugin shutdown complete");
        /// </code></example>
        /// <seealso cref="Result"/>
        public static async Task Shutdown() => await ModIOUnityImplementation.Shutdown(() => { });

        #endregion // Initialization and Maintenance

        #region Authentication

        /// <summary>Listen for an external login attempt. Returns an <see cref="ExternalAuthenticationToken"/> that includes the url and code to display to the user. <c>ExternalAuthenticationToken.task</c> will complete once the user enters the code.</summary>
        /// <remarks>The request will time out after 15 minutes. You can cancel it at any time using <c>token.Cancel()</c>.</remarks>
        /// <example><code>
        /// var response = await ModIOUnityAsync.RequestExternalAuthentication();
        /// if (!response.result.Succeeded())
        /// {
        ///     Debug.Log($"RequestExternalAuthentication failed: {response.result.message}");
        ///
        ///     return;
        /// }
        /// <br />
        /// var token = response.value; // Call token.Cancel() to cancel the authentication
        /// <br />
        /// Debug.Log($"Go to {token.url} in your browser and enter '{token.code}' to login.");
        /// <br />
        /// Result resultToken = await token.task;
        /// <br />
        /// Debug.Log(resultToken.Succeeded() ? "Authentication successful" : "Authentication failed (possibly timed out)");
        /// </code></example>
        /// <seealso cref="Result"/>
        /// <seealso cref="ResultAnd"/>
        /// <seealso cref="ExternalAuthenticationToken"/>
        public static async Task<ResultAnd<ExternalAuthenticationToken>> RequestExternalAuthentication() => await ModIOUnityImplementation.BeginWssAuthentication();

        /// <summary>
        /// Sends an email with a security code to the specified Email Address. The security code
        /// is then used to Authenticate the user session using ModIOUnity.SubmitEmailSecurityCode()
        /// </summary>
        /// <remarks>
        /// The operation will return a Result object.
        /// If the email is successfully sent Result.Succeeded() will equal true.
        /// If you haven't Initialized the plugin then Result.IsInitializationError() will equal
        /// true. If the string provided for the emailaddress is not .NET compliant
        /// Result.IsAuthenticationError() will equal true.
        /// </remarks>
        /// <param name="emailaddress">the Email Address to send the security code to, eg "JohnDoe@gmail.com"</param>
        /// <seealso cref="SubmitEmailSecurityCode"/>
        /// <seealso cref="Result"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///     Result result = await ModIOUnityAsync.RequestAuthenticationEmail("johndoe@gmail.com");
        ///
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
        public static async Task<Result> RequestAuthenticationEmail(string emailaddress)
        {
            return await ModIOUnityImplementation.RequestEmailAuthToken(emailaddress);
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
        /// <seealso cref="RequestAuthenticationEmail"/>
        /// <seealso cref="Result"/>
        /// <example><code>
        /// async void Example(string userSecurityCode)
        /// {
        ///     Result result = await ModIOUnityAsync.SubmitEmailSecurityCode(userSecurityCode);
        ///
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
        public static async Task<Result> SubmitEmailSecurityCode(string securityCode)
        {
            return await ModIOUnityImplementation.SubmitEmailSecurityCode(securityCode);
        }
        /// <summary>
        /// This retrieves the terms of use text to be shown to the user to accept/deny before
        /// authenticating their account via a third party provider, eg steam or google.
        /// </summary>
        /// <remarks>
        /// If the operation succeeds it will also provide a TermsOfUse struct that contains a
        /// TermsHash struct which you will need to provide when calling a third party
        /// authentication method such as ModIOUnity.AuthenticateUserViaSteam()
        /// </remarks>
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
        /// <example><code>
        /// async void Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        public static async Task<ResultAnd<TermsOfUse>> GetTermsOfUse()
        {
            return await ModIOUnityImplementation.GetTermsOfUse();
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
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaSteam(steamToken, "johndoe@gmail.com", modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaSteam(string steamToken,
                                                                  string emailAddress,
                                                                  TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(
                steamToken, AuthenticationServiceProvider.Steam, emailAddress, hash, null, null,
                null, 0);
        }

        /// <summary>
        /// Attempts to authenticate a user via the epic API.
        /// </summary>
        /// <param name="epicToken">the user's epic token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <seealso cref="ModIOUnity.AuthenticateUserViaEpic"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaEpic(epicToken, "johndoe@gmail.com", modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaEpic(string epicToken,
                                                                  string emailAddress,
                                                                  TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(
                epicToken, AuthenticationServiceProvider.Steam, emailAddress, hash, null, null,
                null, 0);
        }

        /// <summary>
        /// Attempts to authenticate a user via the steam API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="authCode">the user's authcode token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <param name="environment">the PSN account environment</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaPlayStation(authCode, "johndoe@gmail.com", modIOTermsOfUse.hash, PlayStationEnvironment.np);
        ///
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
        public static async Task<Result> AuthenticateUserViaPlayStation(string authCode,
                                                                  string emailAddress,
                                                                  TermsHash? hash,
                                                                  PlayStationEnvironment environment)
        {
            return await ModIOUnityImplementation.AuthenticateUser(
                authCode, AuthenticationServiceProvider.PlayStation, emailAddress, hash, null, null,
                null, environment);
        }

        /// <summary>
        /// Attempts to authenticate a user via the GOG API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="gogToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaGOG(gogToken, "johndoe@gmail.com", modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaGOG(string gogToken, string emailAddress,
                                                                TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(gogToken, AuthenticationServiceProvider.GOG,
                emailAddress, hash, null, null, null, 0);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Itch.io API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="itchioToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaItch(itchioToken, "johndoe@gmail.com", modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaItch(string itchioToken,
                                                                 string emailAddress,
                                                                 TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(
                itchioToken, AuthenticationServiceProvider.Itchio, emailAddress, hash, null, null,
                null, 0);
        }

        /// <summary>
        /// Attempts to authenticate a user via the Xbox API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="xboxToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaItch(xboxToken, "johndoe@gmail.com", modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaXbox(string xboxToken,
                                                                 string emailAddress,
                                                                 TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(xboxToken, AuthenticationServiceProvider.Xbox,
                emailAddress, hash, null, null, null, 0);
        }

        /// <summary>
        /// Attempts to authenticate a user via the switch API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="switchToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaItch(switchToken, "johndoe@gmail.com", modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaSwitch(string switchToken,
                                                                   string emailAddress,
                                                                   TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(
                switchToken, AuthenticationServiceProvider.Switch, emailAddress, hash, null, null,
                null, 0);
        }

        /// <summary>
        /// Attempts to authenticate a user via the discord API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="discordToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaDiscord(discordToken, "johndoe@gmail.com", modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaDiscord(string discordToken,
                                                                    string emailAddress,
                                                                    TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(
                discordToken, AuthenticationServiceProvider.Discord, emailAddress, hash, null, null,
                null, 0);
        }

        /// <summary>
        /// Attempts to authenticate a user via the google API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="googleToken">the user's steam token</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaGoogle(googleToken, "johndoe@gmail.com", modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaGoogle(string googleToken,
                                                                   string emailAddress,
                                                                   TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(
                googleToken, AuthenticationServiceProvider.Google, emailAddress, hash, null, null,
                null, 0);
        }

        /// <summary>
        /// Attempts to authenticate a user via the oculus API.
        /// </summary>
        /// <remarks>
        /// You will first need to get the terms of use and hash from the ModIOUnity.GetTermsOfUse()
        /// method.
        /// </remarks>
        /// <param name="oculusToken">the user's oculus token</param>
        /// <param name="oculusDevice">the device you're authenticating on</param>
        /// <param name="nonce">the nonce</param>
        /// <param name="userId">the user id</param>
        /// <param name="emailAddress">the user's email address (Can be null)</param>
        /// <param name="hash">the TermsHash retrieved from ModIOUnity.GetTermsOfUse()</param>
        /// <seealso cref="GetTermsOfUse"/>
        /// <example><code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaOculus(OculusDevice.Quest,
        ///                                                                     nonce,
        ///                                                                     userId,
        ///                                                                     oculusToken,
        ///                                                                     "johndoe@gmail.com",
        ///                                                                     modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaOculus(OculusDevice oculusDevice, string nonce,
                                                                   long userId, string oculusToken,
                                                                   string emailAddress,
                                                                   TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(
                oculusToken, AuthenticationServiceProvider.Oculus, emailAddress, hash, nonce,
                oculusDevice, userId.ToString(), 0);
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
        /// <seealso cref="GetTermsOfUse"/>
        /// <code>
        /// // First we get the Terms of Use to display to the user and cache the hash
        /// async void GetTermsOfUse_Example()
        /// {
        ///     ResultAnd&#60;TermsOfUser&#62; response = await ModIOUnityAsync.GetTermsOfUse();
        ///
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
        /// async void Authenticate_Example()
        /// {
        ///     Result result = await ModIOUnityAsync.AuthenticateUserViaOpenId(idToken,
        ///                                                                     "johndoe@gmail.com",
        ///                                                                     modIOTermsOfUse.hash);
        ///
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
        public static async Task<Result> AuthenticateUserViaOpenId(string idToken,
                                                                   string emailAddress,
                                                                   TermsHash? hash)
        {
            return await ModIOUnityImplementation.AuthenticateUser(
                idToken, AuthenticationServiceProvider.OpenId, emailAddress, hash, null,
                null, null, 0);
        }

        /// <summary>
        /// Informs you if the current user session is authenticated or not.
        /// </summary>
        /// <seealso cref="Result"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///     Result result = await ModIOUnityAsync.IsAuthenticated();
        ///
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
        public static async Task<Result> IsAuthenticated()
        {
            return await ModIOUnityImplementation.IsAuthenticated();
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
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="TagCategory"/>
        /// <seealso cref="Result"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///     ResultAnd&#60;TagCategory[]&#62; response = await ModIOUnityAsync.GetTagCategories();
        ///
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
        public static async Task<ResultAnd<TagCategory[]>> GetTagCategories()
        {
            return await ModIOUnityImplementation.GetGameTags();
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
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="ModPage"/>
        /// <seealso cref="Result"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///     SearchFilter filter = new SearchFilter();
        ///     filter.SetPageIndex(0);
        ///     filter.SetPageSize(10);
        ///     ResultAnd&#60;ModPage&#62; response = await ModIOUnityAsync.GetMods(filter);
        ///
        ///     if (response.result.Succeeded())
        ///     {
        ///         Debug.Log("ModPage has " + response.value.modProfiles.Length + " mods");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get mods");
        ///     }
        /// }
        /// </code></example>
        public static async Task<ResultAnd<ModPage>> GetMods(SearchFilter filter)
        {
            return await ModIOUnityImplementation.GetMods(filter);
        }

        /// <summary>
        /// Requests a single ModProfile from the mod.io server by its ModId.
        /// </summary>
        /// <remarks>
        /// If there is a specific mod that you want to retrieve from the mod.io database you can
        /// use this method to get it.
        /// </remarks>
        /// <param name="modId">the ModId of the ModProfile to get</param>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModProfile"/>
        /// <seealso cref="Result"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///     ModId modId = new ModId(1234);
        ///     ResultAnd&#60;ModProfile&#62; response = await ModIOUnityAsync.GetMod(modId);
        ///
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
        public static async Task<ResultAnd<ModProfile>> GetMod(ModId modId)
        {
            return await ModIOUnityImplementation.GetMod(modId.id);
        }

        public static Task<ResultAnd<ModProfile>> GetModSkipCache(ModId modId) => ModIOUnityImplementation.GetModSkipCache(modId.id);

        /// <summary>
        /// Get all comments posted in the mods profile. Successful request will return an array of
        /// Comment Objects. We recommended reading the filtering documentation to return only the
        /// records you want.
        /// </summary>
        /// <param name="modId">the ModId of the comments to get</param>
        /// <param name="filter">The filter to apply when searching through comments (can only apply
        /// pagination parameters, Eg. page size and page index)</param>
        /// <seealso cref="CommentPage"/>
        /// <seealso cref="ModComment"/>
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ResultAnd"/>
        /// <seealso cref="ModIOUnity.GetModComments"/>
        public static async Task<ResultAnd<CommentPage>> GetModComments(ModId modId, SearchFilter filter)
        {
            return await ModIOUnityImplementation.GetModComments(modId, filter);
        }

        /// <summary>
        /// Retrieves a list of ModDependenciesObjects that represent mods that depend on a mod.
        /// </summary>
        /// <remarks>
        /// This function returns only immediate mod dependencies, meaning that if you need the dependencies for the dependent
        /// mods, you will have to make multiple calls and watch for circular dependencies.
        /// </remarks>
        /// <seealso cref="ModId"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ResultAnd"/>
        /// <seealso cref="ModDependenciesObject"/>
        /// <seealso cref="ModIOUnity.GetModDependencies"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///     ModId modId = new ModId(1234);
        ///     var resultAnd = await ModIOUnityAsync.GetModDependencies(modId);
        ///
        ///     if (resultAnd.result.Succeeded())
        ///     {
        ///         ModDependenciesObject[] modDependenciesObjects = resultAnd.value;
        ///         Debug.Log("retrieved mods dependencies");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("failed to get mod dependencies");
        ///     }
        /// }
        /// </code></example>
        /// <param name="modId"></param>
        /// <param name="commentDetails"></param>
        /// <returns></returns>
        public static async Task<ResultAnd<ModComment>> AddModComment(ModId modId, CommentDetails commentDetails)
        {
            return await ModIOUnityImplementation.AddModComment(modId, commentDetails);
        }

        /// <summary>
        /// Delete a comment from a mod profile. Successful request will return 204 No Content and fire a MOD_COMMENT_DELETED event.
        /// </summary>
        /// <param name="modId">Id of the mod to add the comment to</param>
        /// <param name="commentId">The id for the comment to be removed</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModComment"/>
        /// <seealso cref="CommentDetails"/>
        /// <seealso cref="DeleteModComment"/>
        /// <seealso cref="ModIOUnity.DeleteModComment"/>
        /// <seealso cref="EditModComment"/>
        /// <example><code>
        ///private ModId modId;
        ///private long commentId;
        ///
        ///void Example()
        ///{
        ///    var result = await ModIOUnityAsync.DeleteModComment(modId, commentId);
        ///    if (result.Succeeded())
        ///    {
        ///        Debug.Log("deleted comment");
        ///    }
        ///    else
        ///    {
        ///        Debug.Log("failed to delete comment");
        ///    }
        ///}
        /// </code></example>
        public static async Task<Result> DeleteModComment(ModId modId, long commentId)
        {
            return await ModIOUnityImplementation.DeleteModComment(modId, commentId);
        }

        /// <summary>
        /// Update a comment for the corresponding mod. Successful request will return the updated Comment Object.
        /// </summary>
        /// <param name="modId">Id of the mod the comment is on</param>
        /// <param name="content">Updated contents of the comment.</param>
        /// <param name="commentId">The id for the comment you wish to edit</param>
        /// <seealso cref="ResultAnd"/>
        /// <seealso cref="ModComment"/>
        /// <seealso cref="ModIOUnity.UpdateModComment"/>
        /// <example><code>
        /// private string content = "This is a Comment";
        /// long commentId = 12345;
        /// ModId modId = (ModId)1234;
        ///
        /// async void UpdateMod()
        /// {
        ///     var response = await ModIOUnityAsync.UpdateModComment(modId, content, commentId);
        ///
        ///     if(response.result.Succeeded())
        ///     {
        ///         Debug.Log("Successfully Updated Comment!");
        ///     }
        ///     else
        ///     {
        ///         Debug.Log("Failed to Update Comment!");
        ///     }
        /// }
        /// </code></example>
        public static async Task<ResultAnd<ModComment>> UpdateModComment(ModId modId, string content, long commentId)
        {
            return await ModIOUnityImplementation.UpdateModComment(modId, content, commentId);
        }

        /// <summary>
        ///
        /// </summary>
        public static async Task<ResultAnd<ModDependencies[]>> GetModDependencies(ModId modId)
        {
            return await ModIOUnityImplementation.GetModDependencies(modId);
        }

        /// <summary>
        /// Get all mod rating's submitted by the authenticated user. Successful request will return an array of Rating Objects.
        /// </summary>
        /// <seealso cref="ModId"/>
        /// <seealso cref="Rating"/>
        /// <seealso cref="ResultAnd"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///    ResultAnd&lt;Rating[]&gt; response = await ModIOUnityAsync.GetCurrentUserRatings();
        ///
        ///    if (response.result.Succeeded())
        ///    {
        ///        foreach(var ratingObject in response.value)
        ///        {
        ///            Debug.Log($"retrieved rating {ratingObject.rating} for {ratingObject.modId}");
        ///        }
        ///    }
        ///    else
        ///    {
        ///        Debug.Log("failed to get ratings");
        ///    }
        /// }
        /// </code></example>
        public static async Task<ResultAnd<Rating[]>> GetCurrentUserRatings()
        {
            return await ModIOUnityImplementation.GetCurrentUserRatings();
        }


        /// <summary>
        /// Gets the rating that the current user has given for a specified mod. You must have an
        /// authenticated session for this to be successful.
        /// </summary>
        /// <remarks>Note that the rating can be 'None'</remarks>
        /// <param name="modId">the id of the mod to check for a rating</param>
        /// <seealso cref="ModRating"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ResultAnd"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///    ModId modId = new ModId(1234);
        ///    ResultAnd&lt;ModRating&gt; response = await ModIOUnityAsync.GetCurrentUserRatingFor(modId);
        ///
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
        public static async Task<ResultAnd<ModRating>> GetCurrentUserRatingFor(ModId modId)
        {
            return await ModIOUnityImplementation.GetCurrentUserRatingFor(modId);
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
        /// <seealso cref="ModRating"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <example><code>
        ///
        /// ModProfile mod;
        ///
        /// async void Example()
        /// {
        ///     Result result = await ModIOUnityAsync.RateMod(mod.id, ModRating.Positive);
        ///
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
        public static async Task<Result> RateMod(ModId modId, ModRating rating)
        {
            return await ModIOUnityImplementation.AddModRating(modId, rating);
        }

        /// <summary>
        /// Adds the specified mod to the current user's subscriptions.
        /// </summary>
        /// <remarks>
        /// If mod management has been enabled via ModIOUnity.EnableModManagement() then the mod
        /// will be downloaded and installed.
        /// </remarks>
        /// <param name="modId">ModId of the mod you want to subscribe to</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModIOUnity.EnableModManagement(ModIO.ModManagementEventDelegate)"/>
        /// <seealso cref="ModIOUnity.GetCurrentModManagementOperation"/>
        /// <example><code>
        ///
        /// ModProfile mod;
        ///
        /// async void Example()
        /// {
        ///     Result result = await ModIOUnityAsync.SubscribeToMod(mod.id);
        ///
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
        public static async Task<Result> SubscribeToMod(ModId modId)
        {
            return await ModIOUnityImplementation.SubscribeTo(modId);
        }

        /// <summary>
        /// Removes the specified mod from the current user's subscriptions.
        /// </summary>
        /// <remarks>
        /// If mod management has been enabled via ModIOUnity.EnableModManagement() then the mod
        /// will be uninstalled at the next opportunity.
        /// </remarks>
        /// <param name="modId">ModId of the mod you want to unsubscribe from</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModIOUnity.EnableModManagement(ModIO.ModManagementEventDelegate)"/>
        /// <seealso cref="ModIOUnity.GetCurrentModManagementOperation"/>
        /// <example><code>
        ///
        /// ModProfile mod;
        ///
        /// async void Example()
        /// {
        ///     Result result = await ModIOUnityAsync.UnsubscribeFromMod(mod.id);
        ///
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
        public static async Task<Result> UnsubscribeFromMod(ModId modId)
        {
            return await ModIOUnityImplementation.UnsubscribeFrom(modId);
        }

        /// <summary>
        /// Gets the current user's UserProfile struct. Containing their mod.io username, user id,
        /// language, timezone and download references for their avatar.
        /// </summary>
        /// <remarks>
        /// This requires the current session to have an authenticated user, otherwise
        /// Result.IsAuthenticationError() from the Result will equal true.
        /// </remarks>
        /// <seealso cref="Result"/>
        /// <seealso cref="UserProfile"/>
        /// <seealso cref="IsAuthenticated"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///     ResultAnd&#60;UserProfile&#62; response = await ModIOUnityAsync.GetCurrentUser();
        ///
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
        public static async Task<ResultAnd<UserProfile>> GetCurrentUser()
        {
            return await ModIOUnityImplementation.GetCurrentUser();
        }

        /// <summary>
        /// Stops any current download and starts downloading the selected mod.
        /// </summary>
        /// <param name="modId">ModId of the mod you want to remove dependencies from</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModIOUnityAsync.DownloadNow"/>
        /// <example><code>
        /// ModId modId;
        /// void Example()
        /// {
        ///     Result result = ModIOUnity.DownloadNow(modId);
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
        public static async void DownloadNow(ModId modId)
        {
            await ModIOUnityImplementation.DownloadNow(modId);
        }

        /// <summary>
        /// Mutes a user which effectively hides any content from that specified user
        /// </summary>
        /// <remarks>The userId can be found from the UserProfile. Such as ModProfile.creator.userId</remarks>
        /// <param name="userId">The id of the user to be muted</param>
        /// <seealso cref="UserProfile"/>
        public static async Task<Result> MuteUser(long userId)
        {
            return await ModIOUnityImplementation.MuteUser(userId);
        }

        /// <summary>
        /// Un-mutes a user which effectively reveals previously hidden content from that user
        /// </summary>
        /// <remarks>The userId can be found from the UserProfile. Such as ModProfile.creator.userId</remarks>
        /// <param name="userId">The id of the user to be muted</param>
        /// <seealso cref="UserProfile"/>
        public static async Task<Result> UnmuteUser(long userId)
        {
            return await ModIOUnityImplementation.UnmuteUser(userId);
        }

        /// <summary>
        /// Gets an array of all the muted users that the current authenticated user has muted.
        /// </summary>
        /// <remarks>This has a cap of 1,000 users. It will not return more then that.</remarks>
        /// <seealso cref="UserProfile"/>
        public static async Task<ResultAnd<UserProfile[]>> GetMutedUsers()
        {
            return await ModIOUnityImplementation.GetMutedUsers();
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
        /// <seealso cref="Result"/>
        /// <seealso cref="ModIOUnity.EnableModManagement(ModIO.ModManagementEventDelegate)"/>
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
        /// <example><code>
        /// async void Example()
        /// {
        ///     Result result = await ModIOUnityAsync.FetchUpdates();
        ///
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
        public static async Task<Result> FetchUpdates()
        {
            return await ModIOUnityImplementation.FetchUpdates();
        }

        /// <summary>
        /// Adds the specified mods as dependencies to an existing mod.
        /// </summary>
        /// <remarks>
        /// If the dependencies already exist they will be ignored and the result will return success
        /// </remarks>
        /// <param name="modId">ModId of the mod you want to add dependencies to</param>
        /// <param name="dependencies">The ModIds that you want to add (max 5 at a time)</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="ModIOUnity.AddDependenciesToMod"/>
        /// <seealso cref="ModIOUnity.RemoveDependenciesFromMod"/>
        /// <seealso cref="ModIOUnityAsync.RemoveDependenciesFromMod"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///     var dependencies = new List&#60;ModId&#62;
        ///     {
        ///         (ModId)1001,
        ///         (ModId)1002,
        ///         (ModId)1003
        ///     };
        ///     Result result = await ModIOUnityAsync.AddDependenciesToMod(mod.id, dependencies);
        ///
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
        public static async Task<Result> AddDependenciesToMod(ModId modId, ICollection<ModId> dependencies)
        {
            return await ModIOUnityImplementation.AddDependenciesToMod(modId, dependencies);
        }

        /// <summary>
        /// Removes the specified mods as dependencies for another existing mod.
        /// </summary>
        /// <remarks>
        /// If the dependencies dont exist they will be ignored and the result will return success
        /// </remarks>
        /// <param name="modId">ModId of the mod you want to remove dependencies from</param>
        /// <param name="dependencies">The ModIds that you want to remove (max 5 at a time)</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <seealso cref="dependencies"/>
        /// <seealso cref="ModIOUnity.AddDependenciesToMod"/>
        /// <seealso cref="ModIOUnity.RemoveDependenciesFromMod"/>
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
        ///     Result result = await ModIOUnityAsync.RemoveDependenciesFromMod(mod.id, dependencies);
        ///
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
        public static async Task<Result> RemoveDependenciesFromMod(ModId modId, ICollection<ModId> dependencies)
        {
            return await ModIOUnityImplementation.RemoveDependenciesFromMod(modId, dependencies);
        }

        #endregion // Mod Management

        #region Mod Uploading
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
        /// <seealso cref="ModIOUnity.GenerateCreationToken"/>
        /// <seealso cref="CreationToken"/>
        /// <seealso cref="ModProfileDetails"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModId"/>
        /// <example><code>
        /// ModId newMod;
        /// Texture2D logo;
        /// CreationToken token;
        ///
        /// async void Example()
        /// {
        ///     token = ModIOUnity.GenerateCreationToken();
        ///
        ///     ModProfileDetails profile = new ModProfileDetails();
        ///     profile.name = "mod name";
        ///     profile.summary = "a brief summary about this mod being submitted"
        ///     profile.logo = logo;
        ///
        ///     ResultAnd&#60;ModId&#62; response = await ModIOUnityAsync.CreateModProfile(token, profile);
        ///
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
        public static async Task<ResultAnd<ModId>> CreateModProfile(CreationToken token,
                                                                    ModProfileDetails modProfileDetails)
        {
            return await ModIOUnityImplementation.CreateModProfile(token, modProfileDetails);
        }

        /// <summary>
        /// This is used to edit or change data in an existing mod profile on the mod.io server.
        /// </summary>
        /// <remarks>
        /// You need to assign the ModId of the mod you want to edit inside of the ModProfileDetails
        /// object included in the parameters
        /// </remarks>
        /// <param name="modprofile">the mod profile details to apply to the mod profile being created</param>
        /// <seealso cref="ModProfileDetails"/>
        /// <seealso cref="Result"/>
        /// <example><code>
        /// ModId modId;
        ///
        /// async void Example()
        /// {
        ///     ModProfileDetails profile = new ModProfileDetails();
        ///     profile.modId = modId;
        ///     profile.summary = "a new brief summary about this mod being edited"
        ///
        ///     Result result = await ModIOUnityAsync.EditModProfile(profile);
        ///
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
        public static async Task<Result> EditModProfile(ModProfileDetails modprofile)
        {
            return await ModIOUnityImplementation.EditModProfile(modprofile);
        }

        /// <summary>
        /// Used to upload a mod file to a mod profile on the mod.io server. A mod file is the
        /// actual archive of a mod. This method can be used to update a mod to a newer version
        /// (you can include changelog information in ModfileDetails).
        /// </summary>
        /// <param name="modfile">the mod file and details to upload</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="ModfileDetails"/>
        /// <seealso cref="ArchiveModProfile"/>
        /// <seealso cref="ModIOUnity.GetCurrentUploadHandle"/>
        /// <example><code>
        ///
        /// ModId modId;
        ///
        /// async void Example()
        /// {
        ///     ModfileDetails modfile = new ModfileDetails();
        ///     modfile.modId = modId;
        ///     modfile.directory = "files/mods/mod_123";
        ///
        ///     Result result = await ModIOUnityAsync.UploadModfile(modfile);
        ///
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
        public static async Task<Result> UploadModfile(ModfileDetails modfile)
        {
            return await ModIOUnityImplementation.AddModfile(modfile);
        }



        /// <summary>
        /// This is used to update the logo of a mod or the gallery images. This works very similar
        /// to EditModProfile except it only affects the images.
        /// </summary>
        /// <param name="modProfileDetails">this holds the reference to the images you wish to upload</param>
        /// <seealso cref="ModProfileDetails"/>
        /// <seealso cref="Result"/>
        /// <seealso cref="EditModProfile"/>
        /// <example><code>
        /// ModId modId;
        /// Texture2D newTexture;
        ///
        /// async void Example()
        /// {
        ///     ModProfileDetails profile = new ModProfileDetails();
        ///     profile.modId = modId;
        ///     profile.logo = newTexture;
        ///
        ///     Result result = await ModIOUnityAsync.UploadModMedia(profile);
        ///
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
        public static Task<Result> UploadModMedia(ModProfileDetails modProfileDetails)
        {
            return ModIOUnityImplementation.UploadModMedia(modProfileDetails);
        }

        /// <summary>
        /// <p>Reorder a mod's gallery images. <paramref name="orderedFilenames"/> must represent every entry in <see cref="ModProfile.galleryImages_Original"/> (or any of the size-variant arrays) or the operation will fail.</p>
        /// <p>Returns the updated <see cref="ModProfile"/>.</p>
        /// </summary>
        public static Task<ResultAnd<ModProfile>> ReorderModMedia(ModId modId, string[] orderedFilenames)
        {
            return ModIOUnityImplementation.ReorderModMedia(modId, orderedFilenames);
        }

        /// <summary>
        /// <p>Delete gallery images from a mod. Filenames can be sourced from <see cref="ModProfile.galleryImages_Original"/> (or any of the size-variant arrays).</p>
        /// <p>Returns the updated <see cref="ModProfile"/>.</p>
        /// </summary>
        public static Task<ResultAnd<ModProfile>> DeleteModMedia(ModId modId, string[] filenames)
        {
            return ModIOUnityImplementation.DeleteModMedia(modId, filenames);
        }

        /// <summary>
        /// Removes a mod from being visible on the mod.io server.
        /// </summary>
        /// <remarks>
        /// If you want to delete a mod permanently you can do so from a web browser.
        /// </remarks>
        /// <param name="modId">the id of the mod to delete</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="CreateModProfile"/>
        /// <seealso cref="EditModProfile"/>
        /// <example><code>
        ///
        /// ModId modId;
        ///
        /// async void Example()
        /// {
        ///     Result result = await ModIOUnityAsync.ArchiveModProfile(modId);
        ///
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
        public static async Task<Result> ArchiveModProfile(ModId modId)
        {
            return await ModIOUnityImplementation.ArchiveModProfile(modId);
        }

        /// <summary>
        /// Get all mods the authenticated user added or is a team member of.
        /// Successful request will return an array of Mod Objects. We
        /// recommended reading the filtering documentation to return only
        /// the records you want.
        /// </summary>
        public static async Task<ResultAnd<ModPage>> GetCurrentUserCreations(SearchFilter filter)
        {
            return await ModIOUnityImplementation.GetCurrentUserCreations(filter);
        }

        /// <summary>
        /// Adds the provided tags to the specified mod id. In order for this to work the
        /// authenticated user must have permission to edit the specified mod. Only existing tags
        /// as part of the game Id will be added.
        /// </summary>
        /// <param name="modId">Id of the mod to add tags to</param>
        /// <param name="tags">array of tags to be added</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="DeleteTags"/>
        /// <seealso cref="ModIOUnityAsync.AddTags"/>
        /// <example><code>
        ///
        /// ModId modId;
        /// string[] tags;
        ///
        /// async void Example()
        /// {
        ///     Result result = await ModIOUnity.AddTags(modId, tags);
        ///
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
        public static async Task<Result> AddTags(ModId modId, string[] tags)
        {
            return await ModIOUnityImplementation.AddTags(modId, tags);
        }

        /// <summary>
        /// Deletes the specified tags from the mod. In order for this to work the
        /// authenticated user must have permission to edit the specified mod.
        /// </summary>
        /// <param name="modId">the id of the mod for deleting tags</param>
        /// <param name="tags">array of tags to be deleted</param>
        /// <seealso cref="Result"/>
        /// <seealso cref="AddTags"/>
        /// <seealso cref="ModIOUnityAsync.DeleteTags"/>
        /// <example><code>
        ///
        /// ModId modId;
        /// string[] tags;
        ///
        /// async void Example()
        /// {
        ///     Result result = await ModIOUnity.DeleteTags(modId, tags);
        ///
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
        public static async Task<Result> DeleteTags(ModId modId, string[] tags)
        {
            return await ModIOUnityImplementation.DeleteTags(modId, tags);
        }
        #endregion // Mod Uploading

        #region Multipart

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
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="ModIOUnity.GetMultipartUploadParts"/>
        /// <seealso cref="ModIOUnity.UploadModfile"/>
        /// <example><code>
        /// ModId modId;
        /// string uploadId;
        ///
        /// private void Example()
        /// {
        ///     var response = await ModIOUnityAsync.GetMultipartUploadParts(modId, uploadId);
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
        public static async Task<ResultAnd<PaginatedResponse<MultipartUploadPart>>> GetMultipartUploadParts(ModId modId, string uploadId, SearchFilter filter)
        {
            return await ModIOUnityImplementation.GetMultipartUploadParts(modId, uploadId, filter);
        }

        /// <summary>
        /// Get all upload sessions belonging to the authenticated user for the corresponding mod. Successful request will return an
        /// array of Multipart Upload Part Objects. We recommended reading the filtering documentation to return only the records you want.
        /// The Mutlipart feature is automatically used when uploading a mod via UploadModFile and is limited to one upload at a time.
        /// This function is optional and is provided to allow for more control over uploading large files for those who require it.
        /// </summary>
        /// <param name="modId">the id of the mod</param>
        /// <param name="filter">The filter to apply when searching through comments (can only apply
        /// pagination parameters, Eg. page size and page index)</param>
        /// <seealso cref="SearchFilter"/>
        /// <seealso cref="ModIOUnity.GetMultipartUploadSessions"/>
        /// <seealso cref="ModIOUnity.UploadModfile"/>
        /// <example><code>
        /// ModId modId;
        ///
        /// private void Example()
        /// {
        ///     var response = await ModIOUnity.GetMultipartUploadSessions(modId);
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
        public static async Task<ResultAnd<PaginatedResponse<MultipartUpload>>> GetMultipartUploadSessions(ModId modId, SearchFilter filter)
        {
            return await ModIOUnityImplementation.GetMultipartUploadSessions(modId, filter);
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
        /// <seealso cref="ModIOUnity.AddMultipartUploadParts"/>
        /// <seealso cref="ModIOUnity.UploadModfile"/>
        /// <example><code>
        /// ModId modId;
        /// string uploadId;
        /// string contentRange;
        /// string digest;
        /// byte[] rawBytes;
        ///
        /// private void Example()
        /// {
        ///     var result = await ModIOUnity.AddMultipartUploadParts(modId, uploadId, contentRange, digest, rawBytes);
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
        public static async Task<Result> AddMultipartUploadParts(ModId modId, string uploadId, string contentRange, string digest, byte[] rawBytes)
        {
            return await ModIOUnityImplementation.AddMultipartUploadParts(modId, uploadId, contentRange, digest, rawBytes);
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
        /// <seealso cref="ModIOUnity.CreateMultipartUploadSession"/>
        /// <seealso cref="ModIOUnity.UploadModfile"/>
        /// <example><code>
        /// ModId modId;
        /// string filename;
        /// string nonce;
        ///
        /// private void Example()
        /// {
        ///     var response = await ModIOUnity.CreateMultipartUploadSession(modId, filename, nonce);
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
        public static async Task<ResultAnd<MultipartUpload>> CreateMultipartUploadSession(ModId modId, string filename)
        {
            return await ModIOUnityImplementation.CreateMultipartUploadSession(modId, filename);
        }

        /// <summary>
        /// Terminate an active multipart upload session, a successful request will return 204 No Content.
        /// The Mutlipart feature is automatically used when uploading a mod via UploadModFile and is limited to one upload at a time.
        /// This function is optional and is provided to allow for more control over uploading large files for those who require it.
        /// </summary>
        /// <param name="modId">the id of the mod</param>
        /// <param name="uploadId">A universally unique identifier (UUID) that represents the upload session.</param>
        /// <seealso cref="ModIOUnity.DeleteMultipartUploadSession"/>
        /// <seealso cref="ModIOUnity.UploadModfile"/>
        /// <example><code>
        /// ModId modId;
        /// string uploadId;
        ///
        /// private void Example()
        /// {
        ///     var result = await ModIOUnity.DeleteMultipartUploadSession(modId, uploadId);
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
        public static async Task<Result> DeleteMultipartUploadSession(ModId modId, string uploadId)
        {
            return await ModIOUnityImplementation.DeleteMultipartUploadSession(modId, uploadId);
        }

        /// <summary>
        /// Complete an active multipart upload session, this endpoint assumes that you have already uploaded all individual parts.
        /// A successful request will return a 200 OK response code and return a single Multipart Upload Object.
        /// The Mutlipart feature is automatically used when uploading a mod via UploadModFile and is limited to one upload at a time.
        /// This function is optional and is provided to allow for more control over uploading large files for those who require it.
        /// </summary>
        /// <param name="modId">the id of the mod</param>
        /// <param name="uploadId">A universally unique identifier (UUID) that represents the upload session.</param>
        /// <seealso cref="ModIOUnity.CompleteMultipartUploadSession"/>
        /// <seealso cref="ModIOUnity.UploadModfile"/>
        /// <example><code>
        /// ModId modId;
        /// string uploadId;
        ///
        /// private void Example()
        /// {
        ///     var result = await ModIOUnity.CompleteMultipartUploadSession(modId, uploadId);
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
        public static async Task<Result> CompleteMultipartUploadSession(ModId modId, string uploadId)
        {
            return await ModIOUnityImplementation.CompleteMultipartUploadSession(modId, uploadId);
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
        /// <seealso cref="Result"/>
        /// <seealso cref="DownloadReference"/>
        /// <seealso cref="Texture2D"/>
        /// <example><code>
        ///
        /// ModProfile mod;
        ///
        /// async void Example()
        /// {
        ///     ResultAnd&#60;Texture2D&#62; response = await ModIOUnityAsync.DownloadTexture(mod.logoImage_320x180);
        ///
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
        public static async Task<ResultAnd<Texture2D>> DownloadTexture(DownloadReference downloadReference)
        {
            return await ModIOUnityImplementation.DownloadTexture(downloadReference);
        }
#endif
        public static async Task<ResultAnd<byte[]>> DownloadImage(DownloadReference downloadReference)
        {
            return await ModIOUnityImplementation.GetImage(downloadReference);
        }

        #endregion // Media Download

        #region Reporting

        /// <summary>
        /// Reports a specified mod to mod.io.
        /// </summary>
        /// <param name="report">the object containing all of the details of the report you are sending</param>
        /// <seealso cref="Report"/>
        /// <seealso cref="Result"/>
        /// <example><code>
        /// async void Example()
        /// {
        ///     Report report = new Report(new ModId(123),
        ///                                 ReportType.Generic,
        ///                                 "reporting this mod for a generic reason",
        ///                                 "JohnDoe",
        ///                                 "johndoe@mod.io");
        ///
        ///     Result result = await ModIOUnityAsync.Report(report);
        ///
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
        public static async Task<Result> Report(Report report)
        {
            return await ModIOUnityImplementation.Report(report);
        }

        #endregion // Reporting

        #region Monetization

        public static async Task<ResultAnd<TokenPack[]>> GetTokenPacks() => await ModIOUnityImplementation.GetTokenPacks();

        /// <summary>
        /// Convert an in-game consumable that a user has purchased on Steam, Xbox, or Psn into a users
        /// mod.io inventory. This endpoint will consume the entitlement on behalf of the user against
        /// the portal in which the entitlement resides (i.e. Steam, Xbox, Psn).
        /// </summary>
        /// <seealso cref="Entitlement"/>
        /// <seealso cref="EntitlementObject"/>
        /// <seealso cref="ModIOUnity.SyncEntitlements"/>
        /// <code>
        ///
        /// private async void Example(string token)
        /// {
        ///     var response = await ModIOUnity.SyncEntitlements(token);
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
        public static async Task<ResultAnd<Entitlement[]>> SyncEntitlements()
        {
            return await ModIOUnityImplementation.SyncEntitlements();
        }

        /// <summary>
        /// Get users in a monetization team <see cref="MonetizationTeamAccount"/> for a specific mod
        /// </summary>
        /// <param name="modId">The mod to get users for</param>
        public static async Task<ResultAnd<MonetizationTeamAccount[]>> GetModMonetizationTeam(ModId modId)
        {
            return await ModIOUnityImplementation.GetModMonetizationTeam(modId);
        }

        /// <summary>
        /// Set all <see cref="ModMonetizationTeamDetails"/> for a specific mod
        /// </summary>
        /// <param name="modId">The mod to set users for</param>
        /// <param name="team">All users and their splits</param>
        public static async Task<Result> AddModMonetizationTeam(ModId modId, ICollection<ModMonetizationTeamDetails> team)
        {
            return await ModIOUnityImplementation.AddModMonetizationTeam(modId, team);
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
        /// <param name="idempotent">A unique string. Must be alphanumeric and cannot contain unique characters except for -.</param>
        /// <seealso cref="Result"/>
        /// <code>
        /// string idempotent = $"aUniqueKey";
        /// ModId modId = 1234;
        /// int displayAmount = 12;
        /// async void Example()
        /// {
        ///     var result = await ModIOUnity.PurchaseMod(modId, displayAmount, idempotent);
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
        public static async Task<ResultAnd<CheckoutProcess>> PurchaseMod(ModId modId, int displayAmount, string idempotent)
        {
            return await ModIOUnityImplementation.PurchaseMod(modId, displayAmount, idempotent);
        }

        /// <summary>
        /// Get user's wallet balance
        /// </summary>
        /// <seealso cref="Result"/>
        /// <code>
        ///
        /// void Example()
        /// {
        ///     var result = await ModIOUnity.GetUserWalletBalance(Callback);
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
        public static async Task<ResultAnd<Wallet>> GetUserWalletBalance()
        {
            return await ModIOUnityImplementation.GetUserWalletBalance();
        }
        #endregion
    }
}

#pragma warning restore 4014 // Restore warnings about calling async functions from non-async code
