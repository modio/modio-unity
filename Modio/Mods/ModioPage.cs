namespace Modio.Mods
{
    public class ModioPage<T>
    {
        public readonly T[] Data;
        public readonly int PageSize;
        public readonly long PageIndex;
        public readonly long TotalSearchResults;

        internal ModioPage(T[] data, int pageSize, long pageIndex, long totalSearchResults)
        {
            Data = data;
            PageSize = pageSize;
            PageIndex = pageIndex;
            TotalSearchResults = totalSearchResults;
        }

        // Page Index starts at 0 but we treat it like length here, so add 1
        public bool HasMoreResults() => PageSize * (PageIndex + 1) < TotalSearchResults;
    }
}
