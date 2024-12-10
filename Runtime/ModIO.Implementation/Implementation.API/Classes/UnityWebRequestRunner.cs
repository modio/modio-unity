using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.Platform;
using Newtonsoft.Json;
using Plugins.mod.io.Runtime.ModIO.Implementation.Classes;
using UnityEngine;
using UnityEngine.Networking;

namespace ModIO.Implementation.API
{
    internal class UnityWebRequestRunner : IWebRequestRunner, IWebRequestRunnerDownloadToFile
    {
        static readonly Queue<Func<Task>> LiveTasks = new Queue<Func<Task>>();
        static readonly object QueueLock = new object();
        static bool isRunning;

#region Main Request Handling

        public RequestHandle<Result> Download(string url, Stream downloadTo, ProgressHandle progressHandle)
        {
            var downloadHandler = new DownloadHandlerStream(downloadTo);
            return RunDownload(url, progressHandle, downloadHandler);
        }

        public RequestHandle<Result> Download(string url, string downloadToFilepath, ProgressHandle progressHandle)
        {
            // Note that there's multiple versions of DownloadHandlerFile
            // We have legacy implementations of it for some console platforms which will hijack this call
            var downloadHandler = new DownloadHandlerFile(downloadToFilepath);
            return RunDownload(url, progressHandle, downloadHandler);
        }

        static RequestHandle<Result> RunDownload(string url, ProgressHandle progressHandle, DownloadHandler downloadHandler)
        {
            var handle = new RequestHandle<Result>();
            handle.task = RunDownload(BuildWebRequestForDownload(url, downloadHandler), handle, progressHandle);

            return handle;
        }

        static async Task<Result> RunDownload(UnityWebRequest request, RequestHandle<Result> handle, ProgressHandle progressHandle)
        {
            handle.cancel = request.Abort;
            handle.progress = progressHandle;

            await GetDownloadResponse(request, progressHandle);

            WebRequestManager.ShutdownEvent -= request.Abort;

            Result result;
            if (ModIOUnityImplementation.shuttingDown)
            {
                result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
            }
            else if (request.result != UnityWebRequest.Result.Success)
            {
                result = ResultBuilder.Unknown;
                Logger.Log(LogLevel.Error, $"Unhandled result when downloading"
                                           + $"\n{request.result}\n{request.responseCode}");
            }
            else
                result = await ProcessDownloadResponse(request);

            if (progressHandle != null)
            {
                progressHandle.Failed = !result.Succeeded();
                progressHandle.Completed = true;
            }

            request.Dispose();

            return result;
        }

        public RequestHandle<ResultAnd<T>> Upload<T>(WebRequestConfig config, ProgressHandle progressHandle)
        {
            RequestHandle<ResultAnd<T>> handle = new RequestHandle<ResultAnd<T>>();
            var task = Execute(config, handle, progressHandle);
            handle.task = task;

            return handle;
        }

        public async Task<ResultAnd<TResult>> Execute<TResult>(WebRequestConfig config,
                                                               RequestHandle<ResultAnd<TResult>> handle, ProgressHandle progressHandle)
        {
            UnityWebRequest request = BuildWebRequestCommon(config);

            if (handle != null)
            {
                handle.progress = progressHandle;
                handle.cancel = request.Abort;
            }

            if (config.IsUpload)
                await SendUpload(request, config, progressHandle);
            else
                await SendWebRequest(request, config);

            if (progressHandle != null)
            {
                progressHandle.Progress = 1f;
                progressHandle.Completed = true;
            }

            ResultAnd<TResult> result;
            try
            {
                if (ModIOUnityImplementation.shuttingDown)
                {
                    if (request != null) LogRequestBeingAborted(request, config);
                    result = ResultAnd.Create(ResultCode.Internal_OperationCancelled, default(TResult));
                }
                else
                    result = await ProcessResponse<TResult>(request, config);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, $"Unknown exception caught trying to process"
                                           + $" web request response.\nException: {e.Message}\n"
                                           + $"Stacktrace: {e.StackTrace}");
                result = ResultAnd.Create(ResultCode.Unknown, default(TResult));
            }

            if (progressHandle != null)
                progressHandle.Failed = !result.result.Succeeded();

            if (request != null)
            {
                // this event is added in BuildWebRequest(), we remove it here
                WebRequestManager.ShutdownEvent -= request.Abort;
                request.Dispose();
            }

            return result;
        }
        static Task SendWebRequest(UnityWebRequest request, WebRequestConfig config)
        {
            if (config.RawBodyData != null)
                SetupRequestBodyData(request, config.RawBodyData, "application/json");
            else if (config.HasStringData)
                SetupUrlEncodedRequest(request, config, "application/x-www-form-urlencoded");
            else
            {
                // We still need to set content-type for the server to be happy, but Unity's UploadHandler isn't here to do it
                request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            }

            LogRequestBeingSent(request, config);

            var asyncOperation = request.SendWebRequest();

            if (asyncOperation.isDone)
                return Task.CompletedTask;

            var completionSource = new TaskCompletionSource<bool>();
            asyncOperation.completed += op =>
            {
                completionSource.TrySetResult(true);
            };

            return completionSource.Task;
        }

#endregion


#region Creating WebRequests

        static void LogRequestBeingSent(UnityWebRequest request, WebRequestConfig config)
        {
            string log = $"\n{config.Url}"
                         + $"\nMETHOD: {config.RequestMethodType}"
                         + $"\n{GenerateLogForRequestMessage(request)}"
                         + $"\n{GenerateLogForWebRequestConfig(config)}";
            Logger.Log(LogLevel.Verbose, $"SENDING{log}");
        }
        static void LogRequestBeingSent(UnityWebRequest request)
        {
            string log = $"\n{request.url}"
                         + $"\nMETHOD: {request.method}"
                         + $"\n{GenerateLogForRequestMessage(request)}";
            Logger.Log(LogLevel.Verbose, $"SENDING{log}");
        }

        static void LogRequestBeingAborted(UnityWebRequest request, WebRequestConfig config)
        {
            string log = $"\n{config.Url}"
                         + $"\nMETHOD: {config.RequestMethodType}"
                         + $"\n{GenerateLogForRequestMessage(request)}"
                         + $"\n{GenerateLogForWebRequestConfig(config)}";
            Logger.Log(LogLevel.Verbose, $"ABORTED{log}");
        }

        static async Task<Result> ProcessDownloadResponse(UnityWebRequest request)
        {
            int statusCode = (int)(request.responseCode);

            string completeRequestLog = $"{GenerateLogForStatusCode(statusCode)}"
                                        + $"\n{request.url}"
                                        + $"\nMETHOD: GET"
                                        + $"\n{GenerateLogForRequestMessage(request)}"
                                        + $"\n{GenerateLogForResponseMessage(request)}";

            if (IsSuccessStatusCode(statusCode))
            {
                Logger.Log(LogLevel.Verbose, $"DOWNLOAD SUCCEEDED {completeRequestLog}");
                return ResultBuilder.Success;
            }

            Logger.Log(LogLevel.Verbose, $"DOWNLOAD FAILED [{completeRequestLog}]");
            return await HttpStatusCodeError("binary download omitted", completeRequestLog, statusCode);
        }

        static async Task<ResultAnd<TResult>> ProcessResponse<TResult>(UnityWebRequest request, WebRequestConfig config)
        {
            int statusCode = (int)(request.responseCode);
            string downloadHandlerText = null;

            if (request.downloadHandler != null && statusCode != 204)
                downloadHandlerText = request.downloadHandler.text;

            string completeRequestLog = $"{GenerateLogForStatusCode(statusCode)}"
                                        + $"\n{config.Url}"
                                        + $"\nMETHOD: {config.RequestMethodType}"
                                        + $"\n{GenerateLogForRequestMessage(request)}"
                                        + $"\n{GenerateLogForWebRequestConfig(config)}"
                                        + $"\n{GenerateLogForResponseMessage(request)}";

            if (IsSuccessStatusCode(statusCode))
            {
                Logger.Log(LogLevel.Verbose, $"SUCCEEDED {completeRequestLog}");

                return await FormatResult<TResult>(downloadHandlerText);
            }

            return ResultAnd.Create(await HttpStatusCodeError(downloadHandlerText, completeRequestLog, statusCode), default(TResult));
        }

        static bool IsSuccessStatusCode(int code) => code >= 200 && code < 300;

        static Task GetDownloadResponse(UnityWebRequest request, ProgressHandle progressHandle)
        {
            LogRequestBeingSent(request);

            var asyncOperation = request.SendWebRequest();

            var completionSource = new TaskCompletionSource<bool>();
            asyncOperation.completed += op =>
            {
                completionSource.TrySetResult(true);
            };

            _ = MonitorProgress(request, progressHandle, true);

            return completionSource.Task;
        }
        static async Task MonitorProgress(UnityWebRequest request, ProgressHandle progressHandle, bool monitorDownload)
        {
            float startedAt = Time.unscaledTime;
            ulong lastCalculatedSpeedAtBytes = 0;

            while (progressHandle != null && !request.isDone)
            {
                // Cap the progress, so it doesn't get to 100% while we wait for the server response
                progressHandle.Progress = 0.99f * (monitorDownload ? request.downloadProgress : request.uploadProgress);

                ulong currentBytes = monitorDownload ? request.downloadedBytes : request.uploadedBytes;
                float currentTime = Time.unscaledTime;

                // update BytesPerSecond continuously for the first second, then once per second
                if (currentTime - startedAt > 1 || lastCalculatedSpeedAtBytes == 0)
                {
                    progressHandle.BytesPerSecond = (long)((currentBytes - lastCalculatedSpeedAtBytes) / (currentTime - startedAt));

                    if (currentTime - startedAt > 1)
                    {
                        startedAt = currentTime;
                        lastCalculatedSpeedAtBytes = currentBytes;
                    }
                }

                await Task.Yield();
            }
        }

        static Task SendUpload(UnityWebRequest request, WebRequestConfig config, ProgressHandle progressHandle)
        {
            return EnqueueTask(() =>
            {
                LogRequestBeingSent(request, config);
                if (config.RawBinaryData != null)
                    return SendOctetUploadRequest(request, config, progressHandle);
                return SendMultipartUploadRequest(request, config, progressHandle);
            });
        }

        static Task EnqueueTask(Func<Task> taskFunc)
        {
            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

            lock (QueueLock)
            {
                LiveTasks.Enqueue(async () =>
                {
                    await taskFunc();
                    taskCompletionSource.SetResult(true);
                });

                // TODO: This could cause RunTasks to start on a non-main thread, which will apply to all following tasks until there's a break
                // Is this an issue? It's matching previous behaviour for now
                if (!isRunning)
                    RunTasks();
            }

            return taskCompletionSource.Task;
        }

        static async void RunTasks()
        {
            isRunning = true;
            while (true)
            {
                Func<Task> taskFunc;

                lock (QueueLock)
                {
                    if (LiveTasks.Count == 0)
                    {
                        isRunning = false;
                        break;
                    }

                    taskFunc = LiveTasks.Dequeue();
                }

                await taskFunc();
            }
        }

        static UnityWebRequest BuildWebRequestCommon(WebRequestConfig config)
        {
            // Add API key or Access token
            if (UserData.instance.IsOAuthTokenValid() && !config.DontUseAuthToken)
                config.AddHeader("Authorization", $"Bearer {UserData.instance.oAuthToken}");
            else
                config.Url += $"&api_key={Settings.server.gameKey}";

            var request = new UnityWebRequest(config.Url, config.RequestMethodType, new DownloadHandlerBuffer(), null);
            SetModioHeaders(request);
            SetConfigHeaders(request, config);

            request.timeout = config.ShouldRequestTimeout ? 30 : 0;

            // Add request to shutdown method
            WebRequestManager.ShutdownEvent += request.Abort;
            return request;
        }

        static UnityWebRequest BuildWebRequestForDownload(string url, DownloadHandler downloadHandler)
        {
            Logger.Log(LogLevel.Verbose, $"DOWNLOADING [{url}]");

            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET, downloadHandler, null);
            SetModioHeaders(request);
            request.timeout = 0;

            // Add API key or Access token
            if (UserData.instance?.IsOAuthTokenValid() ?? false)
                request.SetRequestHeader("Authorization", $"Bearer {UserData.instance.oAuthToken}");

            // Add request to shutdown method
            WebRequestManager.ShutdownEvent += request.Abort;

            return request;
        }

        static void SetModioHeaders(UnityWebRequest request)
        {
            // Set default headers for all requests
            request.SetRequestHeader("User-Agent", $"unity-{Application.unityVersion}-{ModIOVersion.Current.ToHeaderString()}");
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader(ServerConstants.HeaderKeys.LANGUAGE, Settings.server.languageCode ?? "en");

            if (PlatformConfiguration.RESTAPI_HEADER != RestApiPlatform.None)
                request.SetRequestHeader(ServerConstants.HeaderKeys.PLATFORM, ServerConstants.ConvertPlatformToHeaderValue(PlatformConfiguration.RESTAPI_HEADER));

            if(Settings.build.userPortal != UserPortal.None)
                request.SetRequestHeader(ServerConstants.HeaderKeys.PORTAL, ServerConstants.ConvertUserPortalToHeaderValue(Settings.build.userPortal));
        }

        static void SetConfigHeaders(UnityWebRequest request, WebRequestConfig config)
        {
            foreach (var header in config.HeaderData)
                request.SetRequestHeader(header.Key, header.Value);
        }

        static void SetupUrlEncodedRequest(UnityWebRequest request, WebRequestConfig config, string contentType)
        {
            string kvpData = "";
            foreach (var kvp in config.StringKvpData)
                kvpData += $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}&";
            kvpData = kvpData.Trim('&');

            SetupRequestBodyData(request, kvpData, contentType);
        }

        static void SetupRequestBodyData(UnityWebRequest request, string data, string contentType)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            var uploadHandler = new UploadHandlerRaw(bytes);
            uploadHandler.contentType = contentType;
            request.uploadHandler = uploadHandler;
        }

        static Task SendMultipartUploadRequest(UnityWebRequest request, WebRequestConfig config, ProgressHandle progressHandle)
        {
            var multipartFormSections = new List<IMultipartFormSection>();

            foreach (var binary in config.BinaryData)
            {
                string contentType = "form-data";
                multipartFormSections.Add(new MultipartFormFileSection(binary.key, binary.data, binary.fileName, contentType));
            }
            foreach (var kvp in config.StringKvpData)
            {
                if (string.IsNullOrEmpty(kvp.Value))
                    continue;

                multipartFormSections.Add(new MultipartFormDataSection(kvp.Key, kvp.Value));
            }

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundaryBytes = Encoding.UTF8.GetBytes(boundary);
            var rawData = UnityWebRequest.SerializeFormSections(multipartFormSections, boundaryBytes);

            var uploadHandler = new UploadHandlerRaw(rawData);
            uploadHandler.contentType = "multipart/form-data; boundary=" + boundary;

            request.uploadHandler = uploadHandler;
            return SendWebRequest(request, progressHandle);
        }

        static Task SendOctetUploadRequest(UnityWebRequest request, WebRequestConfig config, ProgressHandle progressHandle)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");

            var uploadHandler = new UploadHandlerRaw(config.RawBinaryData);
            uploadHandler.contentType = "application/octet-stream; boundary=" + boundary;

            request.uploadHandler = uploadHandler;
            return SendWebRequest(request, progressHandle);
        }

        static async Task SendWebRequest(UnityWebRequest request, ProgressHandle progressHandle)
        {
            request.SendWebRequest();

            await MonitorProgress(request, progressHandle, false);

            while (progressHandle != null && !request.isDone)
            {
                // Cap the progress, so it doesn't get to 100% while we wait for the server response
                progressHandle.Progress = request.uploadProgress * 0.99f;
                await Task.Yield();
            }

            while (!request.isDone) await Task.Yield();
        }

#endregion

#region Processing Response Body

        static async Task<ResultAnd<T>> FormatResult<T>(string rawResponse)
        {
            //int? is used as a nullable type to denote that we are ignoring type in the response
            //ie - some commands are sent without expect any useful response aside from the response code itself
            if (typeof( T ) == typeof( int? ))
            {
                //OnWebRequestResponse
                return ResultAnd.Create(ResultCode.Success, default(T));
            }

            // If the response is empty it was likely 204: NoContent
            if (rawResponse == null)
                return ResultAnd.Create(ResultBuilder.Success, default(T));

            try
            {
                T deserialized = await Task.Run(() => JsonConvert.DeserializeObject<T>(rawResponse));
                return ResultAnd.Create(ResultBuilder.Success, deserialized);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error,
                    $"UNRECOGNISED RESPONSE"
                    + $"\nFailed to deserialize a response from the mod.io server.\nThe data"
                    + $" may have been corrupted or isn't a valid Json format.\n\n[JsonUtility:"
                    + $" {e.Message}] - {e.InnerException}"
                    + $"\nRaw Response: {rawResponse}");

                return ResultAnd.Create(
                    ResultBuilder.Create(ResultCode.API_FailedToDeserializeResponse), default(T));
            }
        }

        #endregion

#region Error Handling

        static async Task<Result> HttpStatusCodeError(string rawResponse, string requestLog, int status)
        {
            var result = await FormatResult<ErrorObject>(rawResponse);

            string errors = GenerateErrorsIntoSingleLog(result.value.error.errors);
            Logger.Log(LogLevel.Error,
                $"HTTP ERROR [{status} {((HttpStatusCode)status).ToString()}]"
                + $"\n Error ref [{result.value.error.code}] {result.value.error.error_ref} - {result.value.error.message}\n{errors}\n\n{requestLog}");

            if (ResultCode.IsInvalidSession(result.value))
            {
                UserData.instance?.SetOAuthTokenAsRejected();
                ResponseCache.ClearCache();

                return ResultBuilder.Create(ResultCode.User_InvalidToken,
                    (uint)result.value.error.error_ref);
            }

            return ResultBuilder.Create(ResultCode.API_FailedToCompleteRequest,
                (uint)result.value.error.error_ref);
        }

#endregion

#region Logging formatting

        static string GenerateLogForWebRequestConfig(WebRequestConfig config)
        {
            string log = "\nFORM BODY\n------------------------\n";
            if (config.StringKvpData.Count > 0)
            {
                log += "String KVPs\n";
                foreach (var kvp in config.StringKvpData)
                    log += $"{kvp.Key}: {kvp.Value}\n";
            }
            else
                log += "--No String Data\n";

            if ((config.BinaryData == null || config.BinaryData.Count > 0) && (config.RawBinaryData == null || config.RawBinaryData.Length > 0))
                log += "--No Binary Data\n";
            else
                log += "Binary files\n";

            if (config.BinaryData != null && config.BinaryData.Count > 0)
            {
                log += "Binary files\n";
                foreach (var binData in config.BinaryData)
                    log += $"{binData.key}: {binData.data.Length} bytes\n";
            }

            if (config.RawBinaryData != null && config.RawBinaryData.Length > 0)
                log += $"Raw Binary data: {config.RawBinaryData.Length}\n";


            return log;
        }

        static string GenerateLogForRequestMessage(UnityWebRequest request)
        {
            if (request == null)
                return "\n\n------------------------ \nWebRequest is null";
            string log = "\n\n------------------------";
            string headers = $"\nREQUEST HEADERS";

            LogHeader("Accept");
            LogHeader("User-Agent");
            LogHeader("Connection");
            LogHeader("accept-language");
            LogHeader("x-modio-platform");
            LogHeader("x-modio-portal");
            LogHeader("Authorization");
            LogHeader("Content-Type");
            LogHeader("x-modio-metrics-secret");

            if (request.uploadHandler != null)
                headers += $"\nuploadHandler.ContentType: {request.uploadHandler.contentType}";

            log += headers;
            return log;

            void LogHeader(string header)
            {
                string requestHeader = request.GetRequestHeader(header);
                if (!string.IsNullOrEmpty(requestHeader))
                {
                    if (header == "Authorization")
                        headers += $"\n{header}: [OAUTH-TOKEN]";
                    else
                        headers += $"\n{header}: {requestHeader}";
                }
            }
        }

        static string GenerateLogForResponseMessage(UnityWebRequest response)
        {
            if (response == null)
                return "\n\n------------------------\n WebResponse is null";

            string log = "\n\n------------------------";
            string headers = $"\nRESPONSE HEADERS";

            var responseHeaders = response.GetResponseHeaders();
            if (responseHeaders != null)
                foreach (var kvp in responseHeaders)
                    headers += $"\n{kvp.Key}: {kvp.Value}";
            else
                headers += "\nNO HEADERS";
            log += headers;
            return log;
        }

        static string GenerateLogForStatusCode(int code) => $"[Http: {code} {(HttpStatusCode)code}]";

        static string GenerateErrorsIntoSingleLog(Dictionary<string, string> errors)
        {
            if (errors == null || errors.Count == 0)
                return "";

            string log = "errors:";
            int count = 1;
            foreach (var error in errors)
            {
                log += $"\n{count}. {error.Key}: {error.Value}";
                count++;
            }

            return log;
        }

 #endregion
    }
}
