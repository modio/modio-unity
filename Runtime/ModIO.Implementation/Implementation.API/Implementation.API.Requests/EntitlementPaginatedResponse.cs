using ModIO.Implementation.API.Objects;

namespace ModIO.Implementation.API.Requests
{
    [System.Serializable]
    public class EntitlementPaginatedResponse<T> : PaginatedResponse<T>
    {
        public EntitlementWalletObject wallet;
    }
}
