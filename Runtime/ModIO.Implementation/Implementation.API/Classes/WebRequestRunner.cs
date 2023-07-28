using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.Platform;
using Newtonsoft.Json;

namespace ModIO.Implementation.API
{
    internal static class WebRequestRunner
    {

#region Main Request Handling
        public static RequestHandle<Result> Download(string url, Stream downloadTo, ProgressHandle progressHandle)
        {
            RequestHandle<Result> handle = new RequestHandle<Result>();
            var task = RunDownload(url, downloadTo, handle, progressHandle);
            handle.task = task;

            return handle;
        }

        static async Task<Result> RunDownload(string url, Stream downloadTo, RequestHandle<Result> handle, ProgressHandle progressHandle)
        {
            Logger.Log(LogLevel.Verbose, $"DOWNLOADING [{url}]");

            Result result = ResultBuilder.Success;

            // Build request
            WebRequest request = null;
            WebResponse response = default;

            // Setup the handle
            handle.progress = progressHandle;

            try
            {
                request = BuildWebRequestForDownload(url);
                handle.cancel = request.Abort;

                response = await request.GetDownloadResponse(downloadTo, progressHandle);
            }
            catch(WebException e)
            {
                response = e.Response;

                if(e.Status == WebExceptionStatus.RequestCanceled)
                {
                    // request?.LogRequestBeingAborted(config);
                    result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
                }
            }
            catch(Exception e)
            {
                if(ModIOUnityImplementation.shuttingDown)
                {
                    result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
                    Logger.Log(LogLevel.Error, $"SHUTDOWN EXCEPTION"
                                               + $"\n{e.Message}\n{e.StackTrace}");
                }
                else
                {
                    result = ResultBuilder.Unknown;
                    Logger.Log(LogLevel.Error, $"Unhandled exception when downloading"
                                               + $"\n{e.Message}\n{e.StackTrace}");
                }
            }

            if (request != null)
            {
                // unsubscribe the web request from the shutdown event
                WebRequestManager.ShutdownEvent -= request.Abort;

            }

            // Process response
            if (result.Succeeded())
            {
                result = await ProcessDownloadResponse(request, response, url);
            }
            else
            {
                Logger.Log(LogLevel.Verbose, $"DOWNLOAD FAILED [{url}]");
            }

            if (!result.Succeeded())
            {
                if(progressHandle != null)
                {
                    progressHandle.Failed = true;
                }
            }

            if(progressHandle != null)
            {
                progressHandle.Completed = true;
            }
            return result;
        }

        public static RequestHandle<ResultAnd<T>> Upload<T>(WebRequestConfig config, ProgressHandle progressHandle)
        {
            RequestHandle<ResultAnd<T>> handle = new RequestHandle<ResultAnd<T>>();
            var task = Execute(config, handle, progressHandle);
            handle.task = task;

            return handle;
        }

        public static async Task<ResultAnd<TResult>> Execute<TResult>(WebRequestConfig config, RequestHandle<ResultAnd<TResult>> handle, ProgressHandle progressHandle)
        {
            ResultAnd<TResult> result = default;
            WebResponse response = null;
            WebRequest request = null;

            if(handle != null)
            {
                handle.progress = progressHandle;
            }

            try
            {
                request = config.IsUpload
                    ? BuildWebRequestForUpload(config, progressHandle)
                    : await BuildWebRequest(config, progressHandle);

                request.Timeout = config.ShouldRequestTimeout ? 30000 : -1;

                if(handle != null)
                {
                    handle.cancel = request.Abort;
                }

                request.LogRequestBeingSent(config);

                response = config.IsUpload
                    ? await request.GetUploadResponse(config, progressHandle)
                    : await request.GetResponseAsync();
            }
            catch(Exception e)
            {
                if(request != null)
                {
                    // this event is added in BuildWebRequest(), we remove it here
                    WebRequestManager.ShutdownEvent -= request.Abort;
                }
                if(e is WebException exception)
                {
                    response = exception.Response;

                    if (exception.Status == WebExceptionStatus.RequestCanceled)
                    {
                        request?.LogRequestBeingAborted(config);
                        return ResultAnd.Create(ResultCode.Internal_OperationCancelled, default(TResult));
                    }
                }
                else
                {
                    response = default;
                }
            }

            if (progressHandle != null)
            {
                progressHandle.Progress = 1f;
                progressHandle.Completed = true;
            }

            try
            {
                if(ModIOUnityImplementation.shuttingDown)
                {
                    request?.LogRequestBeingAborted(config);
                    result = ResultAnd.Create(ResultCode.Internal_OperationCancelled, default(TResult));
                }
                else
                {
                    result = await ProcessResponse<TResult>(request, response, config);
                }
            }
            catch(Exception e)
            {
                if(e is WebException exception)
                {
                    exception.Response?.Close();
                }
                Logger.Log(LogLevel.Error, $"Unknown exception caught trying to process"
                                           + $" web request response.\nException: {e.Message}\n"
                                           + $"Stacktrace: {e.StackTrace}");
                result = ResultAnd.Create(ResultCode.Unknown, default(TResult));
            }

            if(request != null)
            {
                // this event is added in BuildWebRequest(), we remove it here
                WebRequestManager.ShutdownEvent -= request.Abort;
            }

            if(progressHandle != null)
            {
                progressHandle.Failed = !result.result.Succeeded();
            }

            return result;
        }
#endregion


#region Creating WebRequests
        static void LogRequestBeingSent(this WebRequest request, WebRequestConfig config)
        {
            string log = $"\n{config.Url}"
                         + $"\nMETHOD: {config.RequestMethodType}"
                         + $"\n{GenerateLogForRequestMessage(request)}"
                         + $"\n{GenerateLogForWebRequestConfig(config)}";
            Logger.Log(LogLevel.Verbose, $"SENDING{log}");
        }

        static void LogRequestBeingAborted(this WebRequest request, WebRequestConfig config)
        {
            string log = $"\n{config.Url}"
                         + $"\nMETHOD: {config.RequestMethodType}"
                         + $"\n{GenerateLogForRequestMessage(request)}"
                         + $"\n{GenerateLogForWebRequestConfig(config)}";
            Logger.Log(LogLevel.Verbose, $"ABORTED{log}");
        }

        static async Task<Result> ProcessDownloadResponse(WebRequest request, WebResponse response, string url)
        {
            Result finalResult = ResultBuilder.Unknown;

            int statusCode = response == null ? 0 : (int)((HttpWebResponse)response).StatusCode;

            Stream stream = null;

            if (response != null)
            {
                stream = response.GetResponseStream();
            }

            string completeRequestLog = $"{GenerateLogForStatusCode(statusCode)}"
                                        + $"\n{url}"
                                        + $"\nMETHOD: GET"
                                        + $"\n{GenerateLogForRequestMessage(request)}"
                                        + $"\n{GenerateLogForResponseMessage(response)}";

            if(IsSuccessStatusCode(statusCode))
            {
                finalResult = ResultBuilder.Success;
                Logger.Log(LogLevel.Verbose, $"DOWNLOAD SUCCEEDED {completeRequestLog}");
            }
            else
            {
                finalResult = await HttpStatusCodeError(stream, completeRequestLog, statusCode);
                Logger.Log(LogLevel.Verbose, $"DOWNLOAD FAILED [{completeRequestLog}]");
            }

            stream?.Dispose();
            response?.Close();
            return finalResult;
        }

        static async Task<ResultAnd<TResult>> ProcessResponse<TResult>(WebRequest request, WebResponse response, WebRequestConfig config)
        {
            ResultAnd<TResult> finalResult = default;

            int statusCode = response == null ? 0 : (int)((HttpWebResponse)response).StatusCode;
            Stream stream = null;

            // Dont get the stream if there is no content
            if (response != null && statusCode != 204)
            {
                stream = response.GetResponseStream();
            }

            string completeRequestLog = $"{GenerateLogForStatusCode(statusCode)}"
                                        + $"\n{config.Url}"
                                        + $"\nMETHOD: {config.RequestMethodType}"
                                        + $"\n{GenerateLogForRequestMessage(request)}"
                                        + $"\n{GenerateLogForWebRequestConfig(config)}"
                                        + $"\n{GenerateLogForResponseMessage(response)}";

            if(IsSuccessStatusCode(statusCode))
            {
                Logger.Log(LogLevel.Verbose, $"SUCCEEDED {completeRequestLog}");

                finalResult = await FormatResult<TResult>(stream);
            }
            else
            {
                finalResult = ResultAnd.Create(await HttpStatusCodeError(stream, completeRequestLog, statusCode), default(TResult));
            }

            stream?.Dispose();
            response?.Close();
            return finalResult;
        }

        static bool IsSuccessStatusCode(int code) => code >= 200 && code < 300;

        static async Task<WebResponse> GetDownloadResponse(this WebRequest request, Stream downloadStream, ProgressHandle progressHandle)
        {
            // Send request
            WebResponse response = await request.GetResponseAsync();

            // Read response
            using(Stream responseStream = response.GetResponseStream())
            {
                // Get the total size of the file to download
                long totalSize = response.ContentLength;

                // Create a buffer to read the response stream in chunks
                byte[] buffer = new byte[4096];

                // Initialize progress tracking
                long bytesDownloaded = 0;
                long bytesDownloadedForThisSample = 0;
                int bytesRead = 0;
                Stopwatch progressMeasure = new Stopwatch();
                Stopwatch yieldMeasure = new Stopwatch();
                progressMeasure.Start();
                yieldMeasure.Start();

                // Read the response stream in chunks and write to file
                while((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    downloadStream.Write(buffer, 0, bytesRead);
                    bytesDownloaded += bytesRead;

                    if(progressHandle != null)
                    {
                        bytesDownloadedForThisSample += bytesRead;
                        if(progressMeasure.ElapsedMilliseconds >= 1000)
                        {
                            progressHandle.Progress = (float)((decimal)bytesDownloaded / totalSize);
                            progressHandle.BytesPerSecond = (long)(bytesDownloadedForThisSample * (progressMeasure.ElapsedMilliseconds / 1000f));

                            bytesDownloadedForThisSample = 0;
                            progressMeasure.Restart();
                        }
                        if(yieldMeasure.ElapsedMilliseconds >= 16)
                        {
                            await Task.Yield();
                            yieldMeasure.Restart();
                        }
                    }
                }

                progressMeasure.Stop();
                yieldMeasure.Stop();
            }
            return response;
        }

        static async Task<WebResponse> GetUploadResponse(this WebRequest request, WebRequestConfig config, ProgressHandle progressHandle)
        {
            // Send request
            await request.SetupMultipartRequest(config, progressHandle);

            WebResponse response = await request.GetResponseAsync();

            return response;
        }

        static async Task<WebRequest> BuildWebRequest(WebRequestConfig config, ProgressHandle progressHandle)
        {
            // Add API key or Access token
            if (UserData.instance.IsOAuthTokenValid() && !config.DontUseAuthToken)
            {
                config.AddHeader("Authorization", $"Bearer {UserData.instance.oAuthToken}");
            }
            // Add API key if no access token
            else
            {
                config.Url += $"&api_key={Settings.server.gameKey}";
            }

            // Create request
            HttpWebRequest request = WebRequest.Create(config.Url) as HttpWebRequest;
            request.Method = config.RequestMethodType;
            request.SetModioHeaders();
            request.SetConfigHeaders(config);

            // Add request to shutdown method
            WebRequestManager.ShutdownEvent += request.Abort;

            // URL ENCODED REQUEST
            request.ContentType = "application/x-www-form-urlencoded";

            if(config.HasStringData)
            {
                await request.SetupUrlEncodedRequest(config);
            }

            return request;
        }

        static WebRequest BuildWebRequestForUpload(WebRequestConfig config, ProgressHandle progressHandle)
        {
            // Create request
            HttpWebRequest request = WebRequest.Create(config.Url) as HttpWebRequest;
            request.Method = config.RequestMethodType;
            request.SetModioHeaders();
            request.SetConfigHeaders(config);

            // Add API key or Access token
            // TODO if we dont have an auth token we should abort early, all uploads require an auth token
            if (UserData.instance.IsOAuthTokenValid())
            {
                request.Headers.Add("Authorization", $"Bearer {UserData.instance.oAuthToken}");
            }

            // Add request to shutdown method
            WebRequestManager.ShutdownEvent += request.Abort;

            return request;
        }

        static WebRequest BuildWebRequestForDownload(string url)
        {

            // Create request
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.SetModioHeaders();
            request.Timeout = -1;

            // Add API key or Access token
            if (UserData.instance.IsOAuthTokenValid())
            {
                request.Headers.Add("Authorization", $"Bearer {UserData.instance.oAuthToken}");
            }

            // Add request to shutdown method
            WebRequestManager.ShutdownEvent += request.Abort;

            return request;
        }

        static void SetModioHeaders(this WebRequest webRequest)
        {
            // Set default headers for all requests
            HttpWebRequest request = (HttpWebRequest)webRequest;
            request.Accept = "application/json";
            request.UserAgent = $"unity-{UnityEngine.Application.unityVersion}-{ModIOVersion.Current.ToHeaderString()}";
            // Cloudflare reuses open TCP connections for up to 15 minutes (900 seconds) after the last HTTP request
            request.Connection = "true"; // is set to true by default
            request.Headers.Add(ServerConstants.HeaderKeys.LANGUAGE, Settings.server.languageCode ?? "en");
            request.Headers.Add(ServerConstants.HeaderKeys.PLATFORM, PlatformConfiguration.RESTAPI_HEADER);
            request.Headers.Add(ServerConstants.HeaderKeys.PORTAL, ServerConstants.ConvertUserPortalToHeaderValue(Settings.build.userPortal));
        }

        static void SetConfigHeaders(this WebRequest request, WebRequestConfig config)
        {
            foreach(var header in config.HeaderData)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        static async Task SetupUrlEncodedRequest(this WebRequest request, WebRequestConfig config)
        {
            string kvpData = "";
            foreach(var kvp in config.StringKvpData)
            {
                kvpData += $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}&";
            }
            kvpData = kvpData.Trim('&');

            using (Stream requestStream = request.GetRequestStream())
            {
                using(StreamWriter writer = new StreamWriter(requestStream))
                {
                    await writer.WriteAsync(kvpData);
                }
            }
        }

        static async Task SetupMultipartRequest(this WebRequest request, WebRequestConfig config, ProgressHandle progressHandle)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");

            request.ContentType = "multipart/form-data; boundary=" + boundary;

            MultipartFormDataContent multipartContent = new MultipartFormDataContent(boundary);
            foreach(var binary in config.BinaryData)
            {
                ByteArrayContent bytes = new ByteArrayContent(binary.data);
                multipartContent.Add(bytes, binary.key, binary.fileName);
            }
            foreach(var kvp in config.StringKvpData)
            {
                StringContent stringField = new StringContent(kvp.Value);
                multipartContent.Add(stringField, kvp.Key);
            }

            using (Stream requestStream = request.GetRequestStream())
            {
                using (Stream content = await multipartContent.ReadAsStreamAsync())
                {
                    int bytesRead;
                    long totalBytesRead = 0;
                    long bytesUploadedForThisSample = 0;
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();

                    byte[] buffer = new byte[4096];

                    while((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await requestStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        if(progressHandle != null)
                        {
                            // We make the length 1% longer so it doesnt get to 100% while we wait for the server response
                            progressHandle.Progress = (float)(totalBytesRead / (content.Length * (decimal)1.01f));

                            bytesUploadedForThisSample += bytesRead;
                            if(stopwatch.ElapsedMilliseconds >= 1000)
                            {
                                progressHandle.BytesPerSecond = bytesUploadedForThisSample;
                                bytesUploadedForThisSample = 0;
                                stopwatch.Restart();
                            }
                        }
                    }
                    stopwatch.Stop();
                }
            }
        }
#endregion

#region Processing Response Body

        public async static Task<ResultAnd<T>> FormatResult<T>(Stream response)
        {
            //int? is used as a nullable type to denote that we are ignoring type in the response
            //ie - some commands are sent without expect any useful response aside from the response code itself
            if(typeof(T) == typeof(int?))
            {
                //OnWebrequestResponse
                return ResultAnd.Create(ResultCode.Success, default(T));
            }

            // If the response is empty it was likely 204: NoContent
            if(response == null)
            {
                return ResultAnd.Create(ResultBuilder.Success, default(T));
            }

            try
            {
                T deserialized = await Task.Run(()=> Deserialize<T>(response));
                return ResultAnd.Create(ResultBuilder.Success, deserialized);
            }
            catch(Exception e)
            {
                Logger.Log(LogLevel.Error,
                    $"UNRECOGNISED RESPONSE"
                    + $"\nFailed to deserialize a response from the mod.io server.\nThe data"
                    + $" may have been corrupted or isnt a valid Json format.\n\n[JsonUtility:"
                    + $" {e.Message}] - {e.InnerException}");

                return ResultAnd.Create(
                    ResultBuilder.Create(ResultCode.API_FailedToDeserializeResponse), default(T));
            }
        }

        static T Deserialize<T>(Stream content)
        {
            var serializer = new JsonSerializer();
            using (StreamReader sr = new StreamReader(content))
            using(var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<T>(jsonTextReader);
            }
        }

        static bool IsJson(string input)
        {
            if(input == null)
                return false;

            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}")
                   || input.StartsWith("[") && input.EndsWith("]");
        }
#endregion

#region Error Handling
        static async Task<Result> HttpStatusCodeError(Stream response, string requestLog, int status)
        {
            var result = await FormatResult<ErrorObject>(response);

            string errors = GenerateErrorsIntoSingleLog(result.value.error.errors);
            Logger.Log(LogLevel.Error,
                $"HTTP ERROR [{status} {((HttpStatusCode)status).ToString()}]"
                + $"\n Error ref [{result.value.error.code}] {result.value.error.error_ref} - {result.value.error.message}\n{errors}\n\n{requestLog}");

            if(ResultCode.IsInvalidSession(result.value))
            {
                UserData.instance?.SetOAuthTokenAsRejected();
                ResponseCache.ClearCache();

                return ResultBuilder.Create(ResultCode.User_InvalidToken,
                                                       (uint)result.value.error.error_ref);
            }

            return ResultBuilder.Create(ResultCode.API_FailedToCompleteRequest,
                                                   (uint)result.value.error.error_ref);

        }

        static ResultAnd<TResult> TimeOutError<TResult>(WebRequestConfig requestConfig, WebException ex)
        {
            Logger.Log(LogLevel.Error, $"REQUEST TIMED OUT\nDid not receive a request within {30000} milliseconds. "
                                     + $"Check your Internet connection and/or Firewall settings.\n URL: {requestConfig.Url}\n"
                                     + $"ERROR: {ex.ToString()}");

            var result = ResultBuilder.Create(ResultCode.API_FailedToConnect);
            return ResultAnd.Create(result, default(TResult));
        }

#endregion

#region Logging formatting
        static string GenerateLogForWebRequestConfig(WebRequestConfig config)
        {
            string log = "\nFORM BODY\n------------------------\n";
            if(config.StringKvpData.Count > 0)
            {
                log += "String Kvps\n";
                foreach(var kvp in config.StringKvpData)
                {
                    log += $"{kvp.Key}: {kvp.Value}\n";
                }
            }
            else
            {
                log += "--No String Data\n";
            }
            if(config.BinaryData.Count > 0)
            {
                log += "Binary files\n";
                foreach(var binData in config.BinaryData)
                {
                    log += $"{binData.key}: {binData.data.Length} bytes\n";
                }
            }
            else
            {
                log += "--No Binary Data\n";
            }
            return log;
        }

        static string GenerateLogForRequestMessage(WebRequest request)
        {
            if(request == null)
            {
                return "\n\n------------------------ \nWebRequest is null";
            }
            string log = "\n\n------------------------";
            string headers = $"\nREQUEST HEADERS";
            foreach(var key in request.Headers.AllKeys)
            {
                if(key == "Authorization")
                {
                    headers += $"\nAuthorization: [OAUTHTOKEN]";
                    continue;
                }
                headers += $"\n{key}: {request.Headers[key]}";
            }
            log += headers;
            return log;
        }

        static string GenerateLogForResponseMessage(WebResponse response)
        {
            if(response == null)
            {
                return "\n\n------------------------\n WebResponse is null";
            }

            string log = "\n\n------------------------";
            string headers = $"\nRESPONSE HEADERS";
            foreach(var key in response.Headers.AllKeys)
            {
                headers += $"\n{key}: {response.Headers[key]}";
            }
            log += headers;
            return log;
        }

        static string GenerateLogForStatusCode(int code) => $"[Http: {code} {(HttpStatusCode)code}]";

        static string GenerateErrorsIntoSingleLog(Dictionary<string, string> errors)
        {
            if(errors == null || errors.Count == 0)
            {
                return "";
            }

            string log = "errors:";
            int count = 1;
            foreach(var error in errors)
            {
                log += $"\n{count}. {error.Key}: {error.Value}";
                count++;
            }

            return log;
        }
 #endregion

    }
}
