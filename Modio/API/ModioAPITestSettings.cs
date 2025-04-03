using System.Text.RegularExpressions;

namespace Modio.API
{
    public class ModioAPITestSettings : IModioServiceSettings
    {
        public bool FakeDisconnected;
        public string FakeDisconnectedOnEndpointRegex;
        public float FakeDisconnectedTimeoutDuration;

        public bool RateLimitError;
        public string RateLimitOnEndpointRegex;

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
    }
}
