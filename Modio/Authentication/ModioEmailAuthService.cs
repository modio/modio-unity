using System;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Errors;
using Modio.Users;

namespace Modio.Authentication
{
    /// <summary>
    /// Use for authenticating with email
    /// </summary>
    /// <example>
    /// <code>
    /// GameObject codeWindow;
    ///   
    /// void Awake()
    /// {
    ///     ModioServices.Bind&lt;IModioAuthService&gt;()
    ///                  .FromInstance(new ModioEmailAuthPlatform(), 50);
    /// }
    ///   
    /// async void Authenticate()
    /// {
    ///     Error error = await ModioClient.AuthService.Authenticate(true, "some_email@supercoolemail.com");
    ///   
    ///     if (error)
    ///         Debug.LogError($"Error authenticating with email");
    ///     else
    ///         Debug.Log($"Successfully authenticated");
    /// }
    ///   
    /// Task&lt;string&gt; ShowCodePrompt()
    /// {
    ///     codeWindow.Enable;
    ///   
    ///     // Capture security code here
    ///     string code = await SomeCodeInputLogic();
    ///   
    ///     return code;
    /// }
    /// </code>
    /// </example>
    public class ModioEmailAuthService : IModioAuthService, IGetActiveUserIdentifier, IPotentialModioEmailAuthService
    {
        public bool IsEmailPlatform => true;
        public ModioAPI.Portal Portal => ModioAPI.Portal.None;

        IEmailCodePrompter _codePrompter;

        bool _isAttemptInProgress;
        
        public ModioEmailAuthService(Func<Task<string>> codePrompter)
            => _codePrompter = new EmailCodePrompter(codePrompter);

        public ModioEmailAuthService(IEmailCodePrompter codePrompter) => _codePrompter = codePrompter;
        
        /// <remarks>You must call <see cref="SetCodePrompter"/> before calling <see cref="Authenticate"/> when using this constructor.</remarks>
        public ModioEmailAuthService() { }

        /// <summary>Begins the email authentication process. This will invoke the <c>codePrompter</c> passed either
        /// into the constructor of this class or with <see cref="SetCodePrompter"/> to enter the security code.</summary>
        /// <returns><c>Error.None</c> if the authentication completed successfully.</returns>
        public async Task<Error> Authenticate(
            bool displayedTerms,
            string thirdPartyEmail = null
        ) {
            if (!_isAttemptInProgress)
                _isAttemptInProgress = true;
            else
                return new Error(ErrorCode.USER_AUTHENTICATION_IN_PROGRESS);
            
            Error error = ValidateAttempt();
            if (error) return ReturnErrorAndReset(error);

            (Error requestError, EmailRequestResponse? _) =
                await ModioAPI.Authentication.RequestEmailSecurityCode(new EmailAuthenticationRequest(thirdPartyEmail));

            if (requestError) return ReturnErrorAndReset(requestError);

            string code = await _codePrompter.ShowCodePrompt();

            if (string.IsNullOrEmpty(code)) return ReturnErrorAndReset(new Error(ErrorCode.OPERATION_CANCELLED));

            return await ExchangeCode(code);
        }

        /// <summary>Use this to authenticate with a previously acquired, still valid security code.</summary>
        /// <returns><c>Error.None</c> if the authentication completed successfully.</returns>
        public async Task<Error> AuthenticateWithoutEmailRequest()
        {
            Error error = ValidateAttempt();
            if (error) return ReturnErrorAndReset(error);

            string code = await _codePrompter.ShowCodePrompt();

            if (string.IsNullOrEmpty(code)) return ReturnErrorAndReset(new Error(ErrorCode.OPERATION_CANCELLED));

            return await ExchangeCode(code);
        }

        async Task<Error> ExchangeCode(string code)
        {
            (Error exchangeError, AccessTokenObject? accessTokenObject) =
                await ModioAPI.Authentication.ExchangeEmailSecurityCode(
                    new EmailAuthenticationSecurityCodeRequest(code)
                );

            if (!exchangeError) User.Current.OnAuthenticated(accessTokenObject.Value.AccessToken);

            return ReturnErrorAndReset(exchangeError);
        }

        Error ValidateAttempt()
        {
            if (_codePrompter != null)
                return Error.None;

            ModioLog.Error?.Log($"{typeof(ModioEmailAuthService)} cannot authenticate as no Code Prompter has been set! Call ModioEmailAuthPlatform.SetCodePrompter before calling Authenticate or use a constructor that takes a Code Prompter parameter..");
            return new Error(ErrorCode.NOT_INITIALIZED);

        }

        Error ReturnErrorAndReset(Error error)
        {
            _isAttemptInProgress = false;
            return error;
        }

        public void SetCodePrompter(IEmailCodePrompter codePrompter) => _codePrompter = codePrompter;
        public void SetCodePrompter(Func<Task<string>> codePrompter)
            => _codePrompter = new EmailCodePrompter(codePrompter);

        public Task<string> GetActiveUserIdentifier() => Task.FromResult("user");
        
        class EmailCodePrompter : IEmailCodePrompter
        {
            readonly Func<Task<string>> _codePrompt;

            public EmailCodePrompter(Func<Task<string>> codePrompt) => _codePrompt = codePrompt;

            public Task<string> ShowCodePrompt() => _codePrompt.Invoke();
        }
    }
}
