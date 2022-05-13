using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    internal static class GetModfiles
    {
        public struct ResponseSchema
        {
            public ModfileObject[] data;
            public long result_count;
            public long result_total;
            public long result_limit;
            public long result_offset;
        }

        public static readonly RequestTemplate Template =
            new RequestTemplate { requireAuthToken = true, canCacheResponse = true,
                                  requestResponseType = WebRequestResponseType.Text,
                                  requestMethodType = WebRequestMethodType.GET };
    }
}
