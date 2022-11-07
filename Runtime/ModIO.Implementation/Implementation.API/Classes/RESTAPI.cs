using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using ModIO.Implementation.API.Objects;
using ModIO.Implementation.API.Requests;
using UnityEngine;

namespace ModIO.Implementation.API
{
    internal static class RESTAPI
    {
        private const int WebRequestTimeoutSeconds = 20;

        static HashSet<ProgressHandle> liveProgressHandles = new HashSet<ProgressHandle>();

        /// <summary>a runtime cache of currently active WebRequests</summary>
        static Dictionary<string, object> LiveRequests = new Dictionary<string, object>();

        /// <summary>all downloads for textures waiting to be sent</summary>
        static List<object> TextureDownloads = new List<object>();

        /// <summary>This acts as a flag waiting for a UWR to finish.</summary>
        static HashSet<TaskCompletionSource<bool>> waitingTasks =
            new HashSet<TaskCompletionSource<bool>>();

#region Shutdown

        /// <summary>
        /// Ends all requests that may be waiting for a WebRequest to complete
        /// </summary>
        public static void Shutdown()
        {
            TaskCompletionSource<bool>[] tasks = new TaskCompletionSource<bool>[waitingTasks.Count];
            waitingTasks.CopyTo(tasks);

            foreach(TaskCompletionSource<bool> task in tasks)
            {
                if(task != null)
                {
                    task.TrySetResult(true);
                }
            }

            waitingTasks.Clear();
        }

#endregion // Shutdown

#region Deserializing

        /// <summary>
        /// Attempts to deserialize a string response from a WebRequest into the given Type.
        /// If the task fails it will log a FailedToDeserializeError through ModIOLogger.
        /// </summary>
        /// <typeparam name="T">the Type to attempt to deserialize into</typeparam>
        /// <param name="response">string body returned from a WebRequest</param>
        /// <returns>returns default or null if the process fails</returns>
        public static T DeserializeResponse<T>(string response, out Result result)
        {
            try
            {
                result = ResultBuilder.Success;
                return JsonConvert.DeserializeObject<T>(response);
            }
            catch(Exception e)
            {
                result = ResultBuilder.Create(ResultCode.API_FailedToDeserializeResponse);
                Logger.Log(
                    LogLevel.Error,
                    $"Failed to deserialize a response from the mod.io server. The data"
                        + $" may have been corrupted or isnt a valid Json format.\n\n[JsonUtility:"
                        + $" {e.Message}] - {e.InnerException} - Raw Response: {response}");
                return default;
            }
        }

#endregion // Deserializing

#region Generating UnityWebRequest

        public static ResponseCodeType GetResponseType(UnityWebRequest webRequest, Result result)
        {
#if UNITY_2020_2_OR_NEWER
            switch(webRequest.result)
            {
                case UnityWebRequest.Result.Success: 
                    return result.Succeeded() ? ResponseCodeType.Succeeded
                                                     : ResponseCodeType.ProcessingError;
                case UnityWebRequest.Result.ConnectionError:
                    return ResponseCodeType.NetworkError;
                case UnityWebRequest.Result.ProtocolError:
                    return ResponseCodeType.HttpError;
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.InProgress
                        : return ResponseCodeType.ProcessingError;
                default: 
                    return ResponseCodeType.ProcessingError;

            }
#else
            if(webRequest.error == "Request aborted")
            {
                return ResponseCodeType.AbortRequested;
            }

            if(webRequest.isNetworkError)
            {
                return ResponseCodeType.NetworkError;
            }

            if(webRequest.isHttpError)
            {
                return ResponseCodeType.HttpError;
            }

            if(!webRequest.isDone)
            {
                return ResponseCodeType.ProcessingError;
            }

            if(webRequest.responseCode >= 200 && webRequest.responseCode < 300)
            {
                return result.Succeeded() ? ResponseCodeType.Succeeded
                                          : ResponseCodeType.ProcessingError;
            }

            return ResponseCodeType.ProcessingError;
#endif
        }

        static UnityWebRequest GenerateWebRequest(string url, RequestConfig config,
                                                  [CanBeNull] WWWForm form,
                                                  [CanBeNull] DownloadHandler downloadHandler,
                                                  out string webLayoutLog)
        {
            // Trim trailing slashes
            url = url.Trim('/');
            
            UnityWebRequest webRequest;

            // NOTE: On GDK all POST requests require a form body to be valid.
            form = form ?? new WWWForm();

            // (NOTE) Steve: manually re-setting the request methods because I dont trust Unity (For
            // good reason)
            switch(config.requestMethodType)
            {
                default:
                    webRequest = UnityWebRequest.Get(url);
                    webRequest.method = "GET";
                    break;
                case WebRequestMethodType.POST:
                    webRequest = UnityWebRequest.Post(url, form);
                    webRequest.method = "POST";
                    break;
                case WebRequestMethodType.PUT:
                    webRequest = UnityWebRequest.Post(url, form);
                    webRequest.method = "PUT";
                    break;
                case WebRequestMethodType.DELETE:
                    webRequest = UnityWebRequest.Post(url, form);
                    webRequest.method = "DELETE";
                    break;
            }

            switch(config.requestResponseType)
            {
                case WebRequestResponseType.Texture:
                    webRequest.downloadHandler = new DownloadHandlerTexture();
                    break;
                case WebRequestResponseType.File:
                    webRequest.downloadHandler = downloadHandler;
                    break;
            }

            string headerLog;
            SetRequestHeaders(webRequest, config, out headerLog);

            string formLog = string.Empty;
            if(form != null)
            {
                formLog = "FORM BODY:\n";
                if(form.data != null)
                {
                    formLog +=
                        $"BINARY: {form.data.Length} bytes\n{Encoding.UTF8.GetString(form.data).Replace("&", "\n")}\n";
                }
            }

            if (downloadHandler == null)
            {
                webRequest.timeout = WebRequestTimeoutSeconds;
            }

            if(config.ignoreTimeout)
            {
                webRequest.timeout = 0;
            }

            //---------------------------------------[ LOG APPENDING
            //]------------------------------------------------//
            webLayoutLog = "-------------------[ WEB REQUEST ]--------------------\n\n";
            webLayoutLog += $"WEB REQUEST: {webRequest.method}\n";
            webLayoutLog += $"URL: {webRequest.url}\n";
            webLayoutLog += $"GAME_ID: {Settings.server.gameId}\n";
            webLayoutLog += $"API_KEY: {Settings.server.gameKey}\n\n";
            webLayoutLog += $"Timeout: {webRequest.timeout}\n\n";
            webLayoutLog += "------------------------------------------------------\n\n";
            webLayoutLog += headerLog;
            webLayoutLog += "------------------------------------------------------\n\n";
            webLayoutLog += formLog;
            //--------------------------------------------------------------------------------------------------------//

            return webRequest;
        }

        static void SetRequestHeaders(UnityWebRequest webRequest, RequestConfig template,
                                      out string headerLog)
        {
            headerLog = "HEADERS:\n";

            // LANGUAGE
            webRequest.SetRequestHeader(ServerConstants.HeaderKeys.LANGUAGE,
                                        Settings.server.languageCode ?? "en");
            headerLog +=
                $"{ServerConstants.HeaderKeys.LANGUAGE}: {Settings.server.languageCode ?? "en"}\n";

            // PLUGIN VERSION
            webRequest.SetRequestHeader(ServerConstants.HeaderKeys.PLUGIN_VERSION,
                                        ModIOVersion.Current.ToHeaderString());
            headerLog +=
                $"{ServerConstants.HeaderKeys.PLUGIN_VERSION}: {ModIOVersion.Current.ToHeaderString()}\n";

            // PLATFORM
            webRequest.SetRequestHeader(
                ServerConstants.HeaderKeys.PLATFORM,
                ModIO.Implementation.Platform.PlatformConfiguration.RESTAPI_HEADER);
            headerLog +=
                $"{ServerConstants.HeaderKeys.PLATFORM}: {ModIO.Implementation.Platform.PlatformConfiguration.RESTAPI_HEADER}\n";

            // PORTAL
            webRequest.SetRequestHeader(
                ServerConstants.HeaderKeys.PORTAL,
                ServerConstants.ConvertUserPortalToHeaderValue(Settings.build.userPortal));
            headerLog +=
                $"{ServerConstants.HeaderKeys.PORTAL}: {ServerConstants.ConvertUserPortalToHeaderValue(Settings.build.userPortal)}\n";

            // Add API Key
            // (NOTE) TODO Change this to a header in API v2
            if(template.requestResponseType == WebRequestResponseType.Text)
            {
                webRequest.url += $"&api_key={Settings.server.gameKey}";
            }

            if(!string.IsNullOrWhiteSpace(UserData.instance?.oAuthToken))
            {
                // AUTHENTICATED - ADD OAUTHTOKEN
                webRequest.SetRequestHeader("Authorization",
                                            $"Bearer {UserData.instance.oAuthToken}");

                // Append to log
                headerLog += "USER: [OAUTHTOKEN]\n";
            }
        }

#endregion // Generating UnityWebRequest

#region Request Handling

        static async Task WaitForTurnToSend(UnityWebRequest webRequest)
        {
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            waitingTasks.Add(completionSource);

            TextureDownloads.Add(completionSource.Task);

            if(TextureDownloads.Count > 1)
            {
                // If we run this line here, it will nearly double the time it takes to download
                // images if we do a large batch of them all at once. I'll leave this here just in
                // case we find a use for it on other requests, although binary downloads are
                // handled separately in ModManagement.

                // await (Task)TextureDownloads[TextureDownloads.Count - 2];
            }

            await SendAndWaitForRequest(webRequest, completionSource);

            waitingTasks.Remove(completionSource);

            TextureDownloads.Remove(completionSource.Task);
        }

        static async Task SendAndWaitForRequest(UnityWebRequest webRequest,
                                                TaskCompletionSource<bool> completionSource = null)
        {
            UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();

            TaskCompletionSource<bool> taskCompletion = new TaskCompletionSource<bool>();

            waitingTasks.Add(taskCompletion);

            operation.completed += delegate
            {
                taskCompletion.TrySetResult(true);
            };

            await taskCompletion.Task;

            waitingTasks.Remove(taskCompletion);

            completionSource?.TrySetResult(true);
        }

        static bool RequestIsAlreadyBeingRun(string url)
        {
            return LiveRequests.ContainsKey(url);
        }

        /// <summary>
        /// If a duplicate request is being run (using an identical url) this method can be used to
        /// wait for the duplicate operation to finish and give the same result back as the
        /// duplicate instead of running the same operation again. Make sure to use
        /// RequestIsAlreadyBeingRun(string) before using this method.
        /// </summary>
        /// <param name="url">the url of the request to observe</param>
        /// <typeparam name="T">the Type that the response should be deserialized as</typeparam>
        /// <returns>A ResultAnd<T> object with the result and deserialized response of the
        /// UnityWebRequest</returns>
        static async Task<ResultAnd<T>> GetOngoingRequestResult<T>(string url)
        {
            if(LiveRequests[url] is Task<ResultAnd<T>>)
            {
                // DUPLICATE REQUEST
                // Wait for the dupe request to finish and return its result
                Task<ResultAnd<T>> duplicate = (Task<ResultAnd<T>>)LiveRequests[url];
                await duplicate;
                return duplicate.Result;
            }
            else
            {
                // DUPLICATE TYPE FAILURE
                // duplicate entry found but with incorrect return type
                ResultAnd<T> internalResponse = new ResultAnd<T>();
                internalResponse.result =
                    ResultBuilder.Create(ResultCode.Internal_DuplicateRequestWithDifferingSchemas);
                Logger.Log(LogLevel.Error,
                           "If you see this error please notify someone"
                               + " from the mod.io team. A duplicate WebRequest tried to"
                               + " be sent with differing response schema Types."
                               + $"\nurl: {url}");

                return internalResponse;
            }
        }

        /// <summary>
        /// Does a recursive set of requests until all possible results are retrieved. Hard capped
        /// at 10 requests (1,000 results).
        /// </summary>
        /// <param name="url">The endpoint with relevant filters (But do not include pagination)</param>
        /// <param name="requestTemplate">The template of the request</param>
        /// <typeparam name="T">The data type of the page response schema (Make sure this is the
        /// correct API Object for the response schema relating to the endpoint being
        /// used)</typeparam>
        /// <returns>ResultAnd has the result of the entire operation and an array of all the
        /// retrieved results</returns>
        /// <remarks>Note this only works for GET requests</remarks>
        // TODO(@Steve): Add request limit
        // TODO(@Steve): Implement partial result?
        public static async Task<ResultAnd<T[]>> TryRequestAllResults<T>(
            string url, RequestConfig requestTemplate)
        {
            ResultAnd<T[]> finalResponse = new ResultAnd<T[]>();
            List<T> collatedData = new List<T>();

            long numberOfRequestsMade = 0;
            int pageSize = 100;
            string urlWithPagination = $"{url}&{Filtering.Limit}{pageSize}&{Filtering.Offset}{0}";
            numberOfRequestsMade++;

            ResultAnd<PaginatingRequest<T>> internalResponse =
                await Request<PaginatingRequest<T>>(urlWithPagination, requestTemplate);

            long total =
                internalResponse.result.Succeeded() ? internalResponse.value.result_total : 0;

            while(internalResponse.result.Succeeded())
            {
                // Add results to pool
                collatedData.AddRange(internalResponse.value.data);

                // get offset for next request
                long offset = pageSize * numberOfRequestsMade;

                // check if our offset has reached the total yet
                if(offset >= total)
                {
                    // We've retrieved all results
                    break;
                }

                // check if we've reached 10 pages (1,000 results)
                if(numberOfRequestsMade >= 10)
                {
                    Logger.Log(LogLevel.Warning,
                               "Recursive Paging method "
                                   + "(TryRequestAllResults) has reached it's cap of "
                                   + "1,000 results. Ending now to avoid rate limiting.");
                    break;
                }

                // Generate new url paging
                urlWithPagination =
                    $"{url}&{Filtering.Limit}{pageSize}&{Filtering.Offset}{pageSize * numberOfRequestsMade}";

                // Make next request
                internalResponse =
                    await Request<PaginatingRequest<T>>(urlWithPagination, requestTemplate);

                // increment page for next request
                numberOfRequestsMade++;
            }

            finalResponse.result = internalResponse.result;
            finalResponse.value = collatedData.ToArray();

            return finalResponse;
        }

        /// <summary>
        /// Untyped overload for Request<ResultAnd<T>>.
        /// Just returns result and discards the response (T).
        /// </summary>
        /// <returns>returns the Result of the operation</returns>
        public static async Task<Result> Request(string url, RequestConfig requestTemplate,
                                                 [CanBeNull] WWWForm form = null,
                                                 [CanBeNull] DownloadHandler downloadHandler = null,
                                                 [CanBeNull] ProgressHandle progressHandle = null)
        {
            ResultAnd<bool> resultAnd =
                await Request<bool>(url, requestTemplate, form, downloadHandler, progressHandle);
            return resultAnd.result;
        }

        public static async Task<ResultAnd<T>> Request<T>(
            string url, RequestConfig requestTemplate, [CanBeNull] WWWForm form = null,
            [CanBeNull] DownloadHandler downloadHandler = null,
            [CanBeNull] ProgressHandle progressHandle = null)
        {
            //----------------------------------------------------------------------//
            //                      HANDLE DUPLICATE REQUESTS                       //
            //----------------------------------------------------------------------//
            if(RequestIsAlreadyBeingRun(url))
            {
                return await GetOngoingRequestResult<T>(url);
            }


            //----------------------------------------------------------------------//
            //                              RUN REQUEST                             //
            //----------------------------------------------------------------------//

            // Create a new Task
            Task<ResultAnd<T>> newTask =
                PerformRequest<T>(url, requestTemplate, form, downloadHandler, progressHandle);

            // Add the Task to the pool of ongoing requests
            LiveRequests.Add(url, newTask);

            // Wait for the Task to complete
            await newTask;

            // Remove the Task from the pool of ongoing requests
            LiveRequests.Remove(url);

            return newTask.Result;
        }

        static async Task<ResultAnd<T>> PerformRequest<T>(
            string url, RequestConfig requestTemplate, [CanBeNull] WWWForm form = null,
            [CanBeNull] DownloadHandler downloadHandler = null,
            [CanBeNull] ProgressHandle progressHandle = null)
        {
            // to return
            ResultAnd<T> internalResponse = new ResultAnd<T>();
            internalResponse.result = ResultBuilder.Unknown;

            // Create WebRequest
            string webRequestLog;
            using(UnityWebRequest webRequest = GenerateWebRequest(
                      url, requestTemplate, form, downloadHandler, out webRequestLog))
            {
                DecoupledProgressHandleToWebRequestRunner(new ModIoWebRequest(webRequest), progressHandle);

                //--------------------------------------------------------------------------------//
                //                          SEND -> WAIT -> EXTRACT                               //
                //--------------------------------------------------------------------------------//
                /*          Each case block in this switch will send the webRequest,
                            wait for it to finish, then extract the appropriate response
                */
                string serializedResponse = "None";
                Texture2D textureResponse = null;

                Logger.Log(LogLevel.Message, $"SENDING\n{webRequest.url}\n\n" + webRequestLog);
                
                switch(requestTemplate.requestResponseType)
                {
                    case WebRequestResponseType.Text:
                        await SendAndWaitForRequest(webRequest);
                        serializedResponse = ExtractTextResponse(webRequest,
                                                                 out internalResponse.result);
                        break;
                    case WebRequestResponseType.Texture:
                        await WaitForTurnToSend(webRequest);
                        textureResponse = ExtractTextureResponse(webRequest,
                                                                 out internalResponse.result);
                        break;
                    case WebRequestResponseType.File:

                        // Cache File Download Request with ModManagement
                        ModManagement.currentJob.downloadWebRequest = webRequest;

                        // Wait
                        await SendAndWaitForRequest(webRequest);

                        // Mark as success because we dont need to extract response like the other
                        // cases
                        internalResponse.result = ResultBuilder.Success;

                        // Clean up
                        ModManagement.currentJob.downloadWebRequest = null;
                        break;
                }

                // Check for cancellation
                if(ModIOUnityImplementation.shuttingDown)
                {
                    RequestShutdown(internalResponse);
                }

                //--------------------------------[ LOG PARAMS ]----------------------------------//
                string logTitle = string.Empty;
                LogLevel logLevel = LogLevel.Error;
                string responseLog =
                    $"------------------------------------------------------\nRESPONSE: {serializedResponse}";
                //--------------------------------------------------------------------------------//

                // (NOTE) Steve: If result failed earlier, pass it in here to override a potentially
                // incorrect success code.
                //               I dont think this will ever happen but you never know...
                ResponseCodeType responseCode =
                    GetResponseType(webRequest, internalResponse.result);

                switch(responseCode)
                {
                    //----------------------------------------------------------------------------//
                    //                        UNKNOWN / PROCESSING ERROR //
                    //----------------------------------------------------------------------------//
                    case ResponseCodeType.ProcessingError:
                        logTitle =
                            string.IsNullOrWhiteSpace(webRequest.error)
                                ? $"PROCESSING ERROR [Response Code:{webRequest.responseCode}]\n\n"
                                : $"PROCESSING ERROR [Error Code:{webRequest.responseCode}]\n{webRequest.error}\n\n";

                        logTitle += $"\nurl: {webRequest.url}";

                        internalResponse.result =
                            ResultBuilder.Create(ResultCode.API_FailedToGetResponseFromWebRequest);
                        internalResponse.value = default;
                        break;
                    //--------------------------------------------------------------------------------//
                    //                              NETWORK ERROR //
                    //--------------------------------------------------------------------------------//
                    case ResponseCodeType.NetworkError:
                        logTitle =
                            $"NETWORK ERROR\nA Network error occurred. Check your Internet "
                            + $"connection and/or Firewall settings.\n URL: {webRequest.url}\n "
                            + $"ERROR: {webRequest.error}";

                        internalResponse.result =
                            ResultBuilder.Create(ResultCode.API_FailedToConnect);
                        internalResponse.value = default;
                        break;
                    //--------------------------------------------------------------------------------//
                    //                                HTTP ERROR //
                    //--------------------------------------------------------------------------------//
                    case ResponseCodeType.HttpError:
                        ErrorObject error = DeserializeResponse<ErrorObject>(
                            serializedResponse, out internalResponse.result);

                        if(error.error.code == 0)
                        {
                            error.error.code = webRequest.responseCode;
                        }

                        logTitle =
                            $"HTTP ERROR [{error.error.code}]{(error.error.error_ref >= 10000 ? $"[{error.error.error_ref.ToString()}]" : string.Empty)}: {error.error.message}\n\n";

                        logTitle += $"\nurl: {webRequest.url}";

                        internalResponse.result = ResultBuilder.Create(ResultCode.API_FailedToCompleteRequest,
                                                                       (uint)error.error.error_ref);
                        internalResponse.value = default;

                        //I think this might be it?
                        // Has Access Token been rejected? If so, flag it so we don't re-use it
                        if(ResultCode.IsCacheClearingError(error))
                        {
                            internalResponse.result.code = ResultCode.User_InvalidToken;
                            UserData.instance?.SetOAuthTokenAsRejected();
                            ResponseCache.ClearCache();
                        }

                        break;

                    //--------------------------------------------------------------------------------//
                    //                                  USER ABORT //
                    //--------------------------------------------------------------------------------//
                    case ResponseCodeType.AbortRequested:

                        logTitle = $"User aborted the webrequest.\nurl: {webRequest.url}";
                        internalResponse.result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);

                        break;

                    //--------------------------------------------------------------------------------//
                    //                                  SUCCEEDED //
                    //--------------------------------------------------------------------------------//
                    case ResponseCodeType.Succeeded:

                        ProgressHandleEnsureGracefulSuccessOnWebRequestPrematureDisposure(progressHandle);

                        // deserialize response
                        T processResponse = default;

                        // Assign the correct response
                        switch (requestTemplate.requestResponseType)
                        {
                            case WebRequestResponseType.Text:
                                processResponse =
                                    webRequest.responseCode == 204
                                        ? default
                                        : DeserializeResponse<T>(serializedResponse,
                                                                 out internalResponse.result);
                                break;
                            case WebRequestResponseType.Texture:
                                if (typeof(T) == typeof(Texture2D))
                                {
                                    processResponse = (T)(object)textureResponse;
                                }
                                else
                                {
                                    // TODO Write an public error here. We did a dumb dumb, Generic
                                    // T should == Texture2D
                                }

                                break;
                            case WebRequestResponseType.File:
                                // No response for binary download
                                break;
                        }

                        internalResponse.value =
                            internalResponse.result.Succeeded() ? processResponse : default;

                        if (internalResponse.result.Succeeded())
                        {
                            logLevel = LogLevel.Verbose;
                            logTitle = $"SUCCEEDED\n{webRequest.url}\n\n";
                        }
                        else
                        {
                            logTitle =
                                $"[{internalResponse.result.code}:{internalResponse.result.code_api}] " +
                                $"PROCESSING ERROR\nThe WebRequest appeared to succeed but the response failed " +
                                $"to be processed\n" +
                                $"url: {webRequest.url}\n\n";
                        }

                        break;
                }

                string responseHeaders =
                    $"------------------------------------------------------\nRESPONSE HEADERS:\n";
                
                var webRequestResponseHeaders = webRequest.GetResponseHeaders();
                
                if(webRequestResponseHeaders == null)
                {
                    responseHeaders += "No response headers received";
                }
                else
                {
                    foreach(KeyValuePair<string, string> kvp in webRequestResponseHeaders)
                    {
                        responseHeaders += $"{kvp.Key}: {kvp.Value}\n";
                    } 
                }

                Logger.Log(logLevel, $"{logTitle}{responseHeaders}{responseLog}");

                if (progressHandle != null)
                {
                    UnpairProgressHandle(progressHandle);
                }
            }

            if(ModIOUnityImplementation.shuttingDown)
            {
                // the web request could have failed at different points producing different error codes
                // therefore, ensure we are giving a consistent error code if the plugin is shutting down.
                internalResponse.result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
            }
            return internalResponse;
        }

        // This is a cleanup GOTO if the plugin is being shutdown
        static ResultAnd<T> RequestShutdown<T>(ResultAnd<T> internalResponse)
        {
            internalResponse.value = default;
            internalResponse.result = ResultBuilder.Create(ResultCode.Internal_OperationCancelled);
            return internalResponse; 
        }

        static void ProgressHandleEnsureGracefulSuccessOnWebRequestPrematureDisposure(ProgressHandle progressHandle)
        {
            if (progressHandle != null)
            {
                progressHandle.Completed = true;
                progressHandle.Progress = 1f;
            }
        }

        static string ExtractTextResponse(UnityWebRequest webRequest, out Result result)
        {
            if(webRequest.downloadHandler != null)
            {
                // NOTE(@jackson): This needs to be a try-catch because some download handlers throw
                // exceptions when accessing the .text property. Thanks Unity.
                // NOTE(@Steve) I discovered this is because of DownloadHandler.GetText attempts
                // to decode the byte array received from the request into a UTF8 string.
                // (It attempts to decode the string on {get}, not on response return)
                // see:
                // https://docs.unity3d.com/ScriptReference/Networking.DownloadHandler.GetText.html
                try
                {
                    result = ResultBuilder.Success;
                    return webRequest.downloadHandler.text;
                }
                catch
                {
                    Logger.Log(
                        LogLevel.Error,
                        "WEB REQUEST FAILED\nFailed to retrieve response "
                            + "from WebRequest form. This is likely because the response was "
                            + "corrupted or the WebRequest was Disposed prematurely." +
                            $"\n\nURL:{webRequest.url}");
                }
            }

            result = ResultBuilder.Create(ResultCode.API_FailedToGetResponseFromWebRequest);
            return null;
        }

        static Texture2D ExtractTextureResponse(UnityWebRequest webRequest, out Result result)
        {
            if(webRequest.downloadHandler.isDone)
            {
                try
                {
                    result = ResultBuilder.Success;
                    return ((DownloadHandlerTexture)webRequest.downloadHandler).texture;
                }
                catch
                {
                    Logger.Log(
                        LogLevel.Error,
                        "IMAGE DOWNLOAD FAILED\nFailed to extract Texture from the "
                            + "WebRequest. The response may have been interrupted or corrupted." +
                            $"\n\nURL:{webRequest.url}");
                    result = ResultBuilder.Create(ResultCode.API_FailedToGetResponseFromWebRequest);
                }
            }
            else
            {
                Logger.Log(LogLevel.Error,
                           "IMAGE DOWNLOAD FAILED\nAn error occurred and the download was "
                               + "not able to complete.");
            }

            result = ResultBuilder.Create(ResultCode.API_FailedToGetResponseFromWebRequest);
            return null;
        }

        #endregion // Request Handling

        #region Progress Handle Tracking

        static async void DecoupledProgressHandleToWebRequestRunner(IModIoWebRequest webRequest,
                                                         ProgressHandle progressHandle)
        {
            // Early out
            if(progressHandle == null || webRequest == null)
            {
                return;
            }

            if(!liveProgressHandles.Contains(progressHandle))
            {
                liveProgressHandles.Add(progressHandle);
            }

            progressHandle.CoupleToWebRequest(webRequest, () => !ModIOUnityImplementation.shuttingDown && liveProgressHandles.Contains(progressHandle));

            UnpairProgressHandle(progressHandle);
        }

        /// <summary>
        /// Unpairs a handle so it no longer tracks progress of any WebRequests
        /// </summary>
        static void UnpairProgressHandle(ProgressHandle progressHandle)
        {
            if(liveProgressHandles.Contains(progressHandle))
            {
                liveProgressHandles.Remove(progressHandle);
            }
        }

#endregion // Progress Handle Tracking
    }
}
