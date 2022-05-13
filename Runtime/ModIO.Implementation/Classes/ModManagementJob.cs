using UnityEngine.Networking;

namespace ModIO.Implementation
{
    internal class ModManagementJob
    {
        /// <summary>
        /// This is the handle returned to the user when GetCurrentModManagementOperation is called
        /// </summary>
        public ProgressHandle progressHandle;

        public ModCollectionEntry mod;
        public ModManagementOperationType type;

        public UnityWebRequest downloadWebRequest;
        public IModIOZipOperation zipOperation;
    }
}
