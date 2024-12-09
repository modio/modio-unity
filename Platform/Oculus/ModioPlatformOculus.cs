using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

#if MODIO_OCULUS
using Oculus.Platform;
using Oculus.Platform.Models;
#endif

namespace ModIO.Implementation.Platform
{
    public class ModioPlatformOculus : ModioPlatform, IModioSsoPlatform, IModioVirtualCurrencyPackBrowsablePlatform
    {
        public static void SetAsPlatform()
        {
            ActivePlatform = new ModioPlatformOculus();
        }

        public async void PerformSso(TermsHash? displayedTerms, Action<Result> onComplete, string optionalThirdPartyEmailAddressUsedForAuthentication = null)
        {
#if MODIO_OCULUS
            var user = await MakeOculusRequest(Users.GetLoggedInUser());
            var result = ResultBuilder.Create(ResultCode.User_InvalidToken);

            if (!user.result.Succeeded())
            {
                Logger.Log(LogLevel.Error, $"Error getting Oculus UserId! Unable to authenticate");
                onComplete.Invoke(result);
                return;
            }

            Logger.Log(LogLevel.Verbose, $"Successfully retrieved User Id");

            var nonce = await MakeOculusRequest(Users.GetUserProof());

            if (!nonce.result.Succeeded())
            {
                Logger.Log(LogLevel.Error, $"Error getting Oculus Nonce! Unable to authenticate");
                onComplete.Invoke(result);
                return;
            }

            Logger.Log(LogLevel.Verbose, $"Successfully retrieved User Nonce");

            var accessToken = await MakeOculusRequest(Users.GetAccessToken());

            if (!accessToken.result.Succeeded())
            {
                Logger.Log(LogLevel.Error, $"Error getting Oculus Access Token! Unable to authenticate");
                onComplete.Invoke(result);
                return;
            }

            Logger.Log(LogLevel.Verbose, $"Successfully retrieved User Access Token");

            ModIOUnity.AuthenticateUserViaOculus(
                OculusDevice.Quest,
                nonce.value.Value,
                (long)user.value.ID,
                accessToken.value,
                optionalThirdPartyEmailAddressUsedForAuthentication,
                displayedTerms,
                onComplete
            );
#else
            onComplete.Invoke(ResultBuilder.Unknown);
#endif
        }

#if MODIO_OCULUS
        // Makes a synchronous Oculus request awaitable
        static async Task<ResultAnd<T>> MakeOculusRequest<T>(Request<T> request)
        {
            T output = default;
            Result result = ResultBuilder.Success;
            bool isComplete = false;

            request.OnComplete(message =>
            {
                if (!message.IsError)
                {
                    output = message.Data;
                }
                else
                {
                    Logger.Log(LogLevel.Error, $"Error making Oculus request for {typeof(T)}: {message.GetError().Message}");

                    // TODO: Figure out the actual errors and create result codes to match
                    result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
                }

                isComplete = true;
            });

            while (!isComplete)
            {
                await Task.Yield();
            }

            return ResultAnd.Create(result, output);
        }

        internal static async Task<ResultAnd<long>> GetCurrentUserId()
        {
            var user = await MakeOculusRequest(Users.GetLoggedInUser());

            if (user.result.Succeeded())
                return ResultAnd.Create(ResultCode.Success, (long)user.value.ID);

            Logger.Log(LogLevel.Error, $"Error getting Oculus UserId!");
            return ResultAnd.Create(ResultCode.User_InvalidToken, 0L);
        }
#endif

        public async Task<ResultAnd<PortalSku[]>> GetCurrencyPackSkus()
        {
#if MODIO_OCULUS
            var vcResult = await ModIOUnityAsync.GetGameTokenPacks();

            if (!vcResult.result.Succeeded())
            {
                Logger.Log(LogLevel.Error, $"Error getting product list from Oculus! Please see request error for more info.");
                return ResultAnd.Create(vcResult.result, Array.Empty<PortalSku>());
            }

            var skus = vcResult.value.SelectMany(pack => pack.portals)
                .Where(portal => portal.portal == "OCULUS")
                .Select(portal => portal.sku)
                .ToArray();

            Logger.Log(LogLevel.Verbose, $"Found Skus: {string.Join(",", (IEnumerable<string>)skus)}");

            var skuResult = await MakeOculusRequest(IAP.GetProductsBySKU(skus));

            if (!skuResult.result.Succeeded())
            {
                Logger.Log(LogLevel.Error, $"Error getting product list from Oculus! Please see request error for more info.");
                return ResultAnd.Create(skuResult.result, Array.Empty<PortalSku>());
            }

            Logger.Log(LogLevel.Verbose, $"Found Meta Skus: {string.Join(",", skuResult.value.Select(product => product.Sku).ToList())}");

            List<PortalSku> skuList = new List<PortalSku>();

            foreach (Product product in skuResult.value)
            {
                TokenPack matchingPack = vcResult.value.FirstOrDefault(pack => pack.portals.Any(portal => portal.sku == product.Sku));

                if (matchingPack.amount <= 0)
                {
                    Logger.Log(LogLevel.Message, $"No mod.io Token Pack found that matches SKU {product.Sku}. Skipping SKU.");
                    continue;
                }

                var result = new PortalSku(
                    UserPortal.Oculus,
                    product.Sku,
                    product.Name,
                    product.FormattedPrice,
                    (int)matchingPack.amount
                );

                skuList.Add(result);
            }

            return ResultAnd.Create(ResultCode.Success, skuList.ToArray());
#else
            return ResultAnd.Create(ResultCode.Unknown, Array.Empty<PortalSku>());
#endif
        }

        public Task<Result> OpenCheckoutFlow(PortalSku sku)
        {
            TaskCompletionSource<Result> taskCompletionSource = new TaskCompletionSource<Result>();
#if MODIO_OCULUS
            IAP.LaunchCheckoutFlow(sku.Sku).OnComplete(message =>
            {
                if (message.IsError)
                {
                    Logger.Log(LogLevel.Error, $"{message.GetError().Message}");

                    taskCompletionSource.SetResult(ResultBuilder.Create(ResultCode.Internal_OperationCancelled));
                    return;
                }

                // TODO: Sync S2S entitlements here when the endpoint for Oculus has been implemented

                taskCompletionSource.SetResult(ResultBuilder.Success);
            });

            return taskCompletionSource.Task;
#else
            return Task.FromResult(ResultBuilder.Unknown);
#endif
        }
    }
}
