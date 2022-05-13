namespace ModIO.Implementation.API.Requests
{
    ///< summary>
    /// response schemas that can paginate can inherit this so we can have a single pagination
    /// schema that gets shared across all paging requests (mods, events, subscriptions, comments,
    /// etc)
    /// </summary>
    internal class PaginatingRequest<T>
    {
        public T[] data;

        public long result_total;
        public long result_limit;
        public long result_offset;
        public long result_count;
    }
}
