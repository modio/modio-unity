using UnityEngine.Networking;

namespace ModIO
{
    class ModIoWebRequest : IModIoWebRequest
    {
        public UnityWebRequest unityWebRequest;
        public ModIoWebRequest(UnityWebRequest unityWebRequest)
        {
            this.unityWebRequest = unityWebRequest;
        }

        public bool isDone => unityWebRequest.isDone; 
        public ulong downloadedBytes => unityWebRequest.downloadedBytes;
        public float downloadProgress => unityWebRequest.downloadProgress;
        public float uploadProgress => unityWebRequest.uploadProgress;
        public ulong uploadedBytes => unityWebRequest.uploadedBytes;
        public string GetResponseHeader(string name) => unityWebRequest.GetResponseHeader(name);
    }
}
