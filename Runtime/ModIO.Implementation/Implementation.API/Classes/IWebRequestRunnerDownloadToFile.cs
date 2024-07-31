namespace ModIO.Implementation.API
{
    interface IWebRequestRunnerDownloadToFile
    {
        RequestHandle<Result> Download(string url, string downloadToFilepath, ProgressHandle progressHandle);
    }
}
