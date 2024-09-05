namespace ModIO
{
    public enum SearchFilterPlatformStatus
    {
        PendingOnly = 1,
        LiveAndPending = 2,
    }

    internal static class SearchFilterPlatformStatusExtension
    {
        public static string GetString(this SearchFilterPlatformStatus status) => status switch
        {
            SearchFilterPlatformStatus.PendingOnly => "pending_only",
            SearchFilterPlatformStatus.LiveAndPending => "live_and_pending",
            _ => string.Empty,
        };
    }
}
