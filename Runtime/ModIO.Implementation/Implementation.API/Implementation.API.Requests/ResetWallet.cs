

using UnityEngine;

namespace ModIO.Implementation.API.Requests
{

    internal static class ResetWallet
    {
        public static WebRequestConfig Request(int amount, string paymentMethod)
            => InternalRequest(amount, paymentMethod, "POST");

        public static WebRequestConfig InternalRequest(int amount, string paymentMethodId, string requestType)
        {
            var request = new WebRequestConfig()
            {
                Url = $"https://monetisation-api-staging.moddemo.io/test/wallet/{paymentMethodId}/reset?",

                RequestMethodType = requestType
            };

            Debug.Log("Sending to " + request.Url);

            request.AddField("amount", amount.ToString());


            return request;
        }

    }
}
