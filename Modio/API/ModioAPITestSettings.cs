using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Modio.API
{
    public class ModioAPITestSettings : IModioServiceSettings
    {
        public class FakeUnityApiResponse
        {
            public string JsonResponse;
            public long ResponseCode;
            public Dictionary<string, string> ResponseHeaders = new Dictionary<string, string>();
        }
        
        public bool FakeDisconnected;
        public string FakeDisconnectedOnEndpointRegex;
        public float FakeDisconnectedTimeoutDuration;

        public bool RateLimitError;
        public string RateLimitOnEndpointRegex;
        
        //Not serialized, but useful in tests
        public readonly Dictionary<string, HttpResponseMessage> FakeHttpResponses = new Dictionary<string, HttpResponseMessage>();
        public readonly Dictionary<string, FakeUnityApiResponse> FakeUnityResponses = new Dictionary<string, FakeUnityApiResponse>();

        public bool ShouldFakeDisconnected(string url)
        {
            if (FakeDisconnected) return true;

            return !string.IsNullOrEmpty(FakeDisconnectedOnEndpointRegex) &&
                   Regex.IsMatch(url, FakeDisconnectedOnEndpointRegex);
        }
        
        public bool ShouldFakeRateLimit(string url)
        {
            if (RateLimitError) return true;

            return !string.IsNullOrEmpty(RateLimitOnEndpointRegex) &&
                   Regex.IsMatch(url, RateLimitOnEndpointRegex);
        }
        public HttpResponseMessage GetFakeHttpResponse(string url)
        {
            foreach ((string regex, HttpResponseMessage message) in FakeHttpResponses)
            {
                if (Regex.IsMatch(url, regex))
                    return message;
            }

            return null;
        }

        public FakeUnityApiResponse GetFakeUnityResponse(string url)
        {
            foreach ((string regex, FakeUnityApiResponse message) in FakeUnityResponses)
            {
                if (Regex.IsMatch(url, regex))
                    return message;
            }

            return null;
        }
    }
}
