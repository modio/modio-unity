using System;
using System.Threading.Tasks;
using Modio.API;
using Modio.Authentication;
using Modio.Errors;
using Modio.Platforms.Wss.Operations;
using Modio.Users;
using Modio.Wss.Messages;
using Newtonsoft.Json.Linq;

namespace Modio.Platforms.Wss
{
    public class WssAuthService : IModioAuthService, 
                                  IGetActiveUserIdentifier
    {
        
        public async Task<Error> Authenticate(bool displayedTerms, string thirdPartyEmail = null, bool sync = true)
        {
            if (!ModioClient.Settings.TryGetPlatformSettings(out WssSettings _))
                return new WssError(ErrorCode.WSS_NOT_CONFIGURED);

            if (!ModioServices.TryResolve(out IWssCodeDisplayer codePrompter))
                return new WssError(ErrorCode.WSS_NOT_CONFIGURED);
                
            if (!ModioServices.TryResolve(out WssService wssService))
                return Error.Unknown;

            
            (Error error, WssDeviceLoginResponse loginResponse) = await wssService.DoMessageHandshake<WssDeviceLoginResponse>(WssOperationType.WSS_DEVICE_LOGIN, new WssMessage()
            {
                operation = WssOperationType.WSS_DEVICE_LOGIN,
                context = JToken.FromObject(new WssDeviceLoginRequest())
            });

            if (error)
                return error;
            
            await codePrompter.ShowCodePrompt(loginResponse.code, wssService.StopService);
            
        
            (Error accessTokenError, WssMessage message) =
                await wssService.WaitForMessage(WssOperationType.WSS_ACCESS_TOKEN);

            if (accessTokenError)
                return accessTokenError;
            
            if (!message.TryGetValue(out WssLoginSuccess loginSuccess))
                return new WssError(ErrorCode.WSS_FAILED_TO_DESERIALIZE);

            User.Current.OnAuthenticated(loginSuccess.access_token, loginSuccess.date_expires, sync);

            await codePrompter.HideCodePrompt();
            
            //Currently closing the connection after authentication, no other operations are expected
            await wssService.StopService();
            
            return Error.None;

        }


        public ModioAPI.Portal Portal => ModioAPI.Portal.None;

        public Task<string> GetActiveUserIdentifier() => Task.FromResult("user");

    }

}
