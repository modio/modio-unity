using ModIO.Implementation.API;

namespace ModIO.Implementation
{
    internal class ModManagementJob
    {
        /// <summary>
        /// This is the handle returned to the user when GetCurrentModManagementOperation is called
        /// </summary>
        public ProgressHandle progressHandle;

        public ModCollectionEntry modEntry;
        public ModManagementOperationType type;

        public RequestHandle<Result> downloadWebRequest;
        public IModIOZipOperation zipOperation;
    }
}
