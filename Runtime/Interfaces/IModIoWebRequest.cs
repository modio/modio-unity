namespace ModIO
{
    public interface IModIoWebRequest
    {
        bool isDone { get; }
        ulong downloadedBytes { get; }        
        ulong uploadedBytes { get; }
        float downloadProgress { get; }
        float uploadProgress { get; }
        string GetResponseHeader(string name);
    }
}
