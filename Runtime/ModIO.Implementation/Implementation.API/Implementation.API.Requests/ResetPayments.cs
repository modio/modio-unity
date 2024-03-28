namespace ModIO.Implementation.API.Requests
{
    internal static class ResetPayments
    {
        public static WebRequestConfig Request(ModId? modId, UserProfile? buyer)
            => InternalRequest(modId, buyer, "POST");

        public static WebRequestConfig InternalRequest(ModId? modId, UserProfile? buyer, string requestType)
        {
            var request = new WebRequestConfig()
            {
                Url = $"https://api-staging.moddemo.io/gordonfreeman/monetization/reset-purchases/?",
                RequestMethodType = requestType
            };

            if (modId != null)
            {
                request.AddField("mod_id", modId.Value.id.ToString());
            }

            if (buyer != null)
            {
                request.AddField("buyer_id", buyer.Value.userId.ToString());
            }

            return request;
        }
    }
}
