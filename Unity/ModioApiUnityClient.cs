using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.Interfaces;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Modio.API.SchemaDefinitions;
using Modio.Errors;
using Modio.Users;
using UnityEngine;

namespace Modio.Unity
{
    public class ModioAPIUnityClient : IModioAPIInterface
    {
        string _basePath = string.Empty;
        readonly List<string> _defaultParameters = new List<string>();
        readonly Dictionary<string, string> _pathParameters = new Dictionary<string, string>();
        readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>();
        readonly List<UnityWebRequest> _webRequests = new List<UnityWebRequest>();
        
        CancellationTokenSource _cancellationTokenSource;

        public void SetBasePath(string value) => _basePath = value;

        public void AddDefaultPathParameter(string key, string value) => _pathParameters[key] = value;

        public void RemoveDefaultPathParameter(string key) => _pathParameters.Remove(key);

        public void AddDefaultParameter(string value) => _defaultParameters.Add(value);

        public void RemoveDefaultParameter(string value) => _defaultParameters.Remove(value);
        
        public void ResetConfiguration()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            _defaultParameters.Clear();
            _pathParameters.Clear();
            _defaultHeaders.Clear();
            _basePath = string.Empty;
            
            ModioClient.OnShutdown -= Shutdown;
            ModioClient.OnShutdown += Shutdown;
        }

        void Shutdown()
        {
            _cancellationTokenSource?.Cancel();
        }

        public async Task<(Error, Stream)> DownloadFile(string url, CancellationToken token = default)
        {
            Error testError = await CheckFakeErrorsForTest(url);

            if (testError)
                return (testError, null);
            
            if (string.IsNullOrEmpty(url))
            {
                ModioLog.Error?.Log($"Attempting to download null url");
                return (new HttpError(HttpErrorCode.REQUEST_ERROR), null);
            }

            var downloadRequest = ModioAPIRequest.New(url);
            UnityWebRequest webRequest = CreateWebRequest(downloadRequest, url);

            Error error = EnforceAuthentication(downloadRequest, webRequest);

            if (error)
                return (error, null);

            await LogRequest(webRequest);
            Stream stream;
            CancellationToken cachedShutdownToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

            // NOTE: Downloads aren't rate limited, so we don't check for them here
            try
            {
                if (token == default(CancellationToken))
                    token = cachedShutdownToken;

                error = await SendRequest(webRequest, token);

                if (error) return (error, null);

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    ModioLog.Verbose?.Log(
                        ErrorCode.HTTP_EXCEPTION.GetMessage(
                            $"{webRequest.error} {url}\n{webRequest.downloadHandler.text}"
                        )
                    );

                    return (new Error(ErrorCode.HTTP_EXCEPTION), null);
                }

                DownloadHandler handler = webRequest.downloadHandler;
                string jsonResponse = webRequest.downloadHandler.text;

                if (!(webRequest.responseCode >= 200 && webRequest.responseCode < 300))
                {
                    if (IsResponseConnectionFailure(webRequest.responseCode))
                    {
                        ModioLog.Error?.Log($"Unable to reach mod.io servers {webRequest.responseCode}");
                        ModioAPI.SetOfflineStatus(true);
                        return (new Error(ErrorCode.CANNOT_OPEN_CONNECTION), null);
                    }
                    
                    return (GetErrorAndLogBadResponse(jsonResponse), null);
                }

                if (handler == null)
                    // NO HANDLE!? NO DATA!
                    return (new Error(ErrorCode.NO_DATA_AVAILABLE), null);

                stream = new MemoryStream(handler.data);
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log($"Exception in {url}: {e}");

                return (new ErrorException(e), null);
            }

            return (Error.None, stream);
        }
        
        Task<Error> CheckFakeErrorsForTest(string url)
        {
            var testSettings = ModioClient.Settings.GetPlatformSettings<ModioAPITestSettings>();
            
            if(testSettings == null)
                return Task.FromResult(Error.None);

            if (testSettings.ShouldFakeDisconnected(url))
                return FakeConnectionError();

            if (testSettings.ShouldFakeRateLimit(url))
                return Task.FromResult<Error>(new RateLimitError(RateLimitErrorCode.RATELIMITED, 42));

            return Task.FromResult(Error.None);

            async Task<Error> FakeConnectionError()
            {
                await Task.Delay((int)(testSettings.FakeDisconnectedTimeoutDuration * 1000));
            
                return new Error(ErrorCode.CANNOT_OPEN_CONNECTION);
            }
        }

        public void SetDefaultHeader(string name, string value) => _defaultHeaders[name] = value;

        public void RemoveDefaultHeader(string name) => _defaultHeaders.Remove(name);

        UnityWebRequest CreateWebRequest(ModioAPIRequest request, string target)
        {
            var webRequest = new UnityWebRequest(target, MapMethod(request.Method))
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };

            foreach (KeyValuePair<string, string> header in _defaultHeaders)
            {
                webRequest.SetRequestHeader(header.Key, header.Value);
            }
            
            webRequest.SetRequestHeader("User-Agent", Version.GetCurrent());
            webRequest.uploadHandler = MapUploadHandler(request);

            if (webRequest.uploadHandler == null)
                webRequest.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            foreach (KeyValuePair<string, string> headerParameter in request.Options.HeaderParameters)
            {
                webRequest.SetRequestHeader(headerParameter.Key, headerParameter.Value);
            }
            
            foreach (KeyValuePair<string, string> header in _defaultHeaders)
            {
                request.Options.HeaderParameters[header.Key] = header.Value;
            }

            return webRequest;
        }
        
        static Error EnforceAuthentication(ModioAPIRequest downloadRequest, UnityWebRequest webRequest)
        {
           
            if (downloadRequest.Options.RequiresAuthentication && !User.Current.IsAuthenticated)
                return new Error(ErrorCode.USER_NOT_AUTHENTICATED);

            if(User.Current.IsAuthenticated)
                webRequest.SetRequestHeader("Authorization", $"Bearer {User.Current?.GetAuthToken()}");

            return Error.None;
        }

        async Task<(Error error, T)> GetJson<T>(ModioAPIRequest request, Func<JsonTextReader, Task<T>> reader)
        {
            string target = BuildPath(request);
            
            Error error = await CheckFakeErrorsForTest(target);
            if(error)
                return (error, default(T));
            
            using UnityWebRequest webRequest = CreateWebRequest(request, target);

            error = EnforceAuthentication(request, webRequest);

            if (error)
                return (error, default(T));

            _webRequests.Add(webRequest);

            CancellationToken cachedShutdownToken = _cancellationTokenSource?.Token ?? CancellationToken.None;
            
            try
            {
                await LogRequest(webRequest, request);
                error = await SendRequest(webRequest, cachedShutdownToken);

                if(error) return (error, default(T));
                
                string jsonResponse = webRequest.downloadHandler.text;

                if (webRequest.responseCode == 204) return (Error.None, (T)(object)new Response204());

                ModioLog.Verbose?.Log(jsonResponse);

                if (webRequest.responseCode < 200 || webRequest.responseCode >= 300)
                {
                    if (IsResponseConnectionFailure(webRequest.responseCode))
                    {
                        ModioLog.Error?.Log($"Unable to reach mod.io servers {webRequest.responseCode}");
                        ModioAPI.SetOfflineStatus(true);
                        return (new Error(ErrorCode.CANNOT_OPEN_CONNECTION), default(T));
                    }

                    if (webRequest.responseCode == 429
                        && webRequest.GetResponseHeaders().TryGetValue("retry-after", out string retryHeader)
                        && !string.IsNullOrEmpty(retryHeader)
                        && int.TryParse(retryHeader, out int retryAfterSeconds))
                    {
                        GetErrorAndLogBadResponse(jsonResponse);
                        return (new RateLimitError(RateLimitErrorCode.RATELIMITED, retryAfterSeconds), default(T));
                    }
                    
                    return (GetErrorAndLogBadResponse(jsonResponse), default(T));
                }

                if (ModioAPI.IsOffline) ModioAPI.SetOfflineStatus(false);

                using var stringReader = new StringReader(jsonResponse);
                using var jsonTextReader = new JsonTextReader(stringReader);
                return (Error.None, await reader(jsonTextReader));
            }
            catch (JsonException e)
            {
                ModioLog.Verbose?.Log(ErrorCode.HTTP_EXCEPTION.GetMessage($"{target}\n{e}"));
                return (new Error(ErrorCode.INVALID_JSON), default(T));
            }
            catch (Exception e)
            {
                ModioLog.Error?.Log(e.GetType());
                return (new Error(ErrorCode.INVALID_JSON), default(T));
            }
            finally
            {
                _webRequests.Remove(webRequest);
            }
        }

        static Error GetErrorAndLogBadResponse(string jsonResponse)
        {
            // It's possible (though hopefully rare) for the server to return a different error before our JSON ErrorObject
            if (!string.IsNullOrEmpty(jsonResponse) && jsonResponse[0] != '{')
            {
                int firstOpenBracketIndex = jsonResponse.IndexOf('{');

                if (firstOpenBracketIndex > 0)
                {
                    string serverError = jsonResponse.Substring(0, firstOpenBracketIndex);
                    ModioLog.Verbose?.Log($"Unexpected error from server before JSON: {serverError}");
                    jsonResponse = jsonResponse.Substring(firstOpenBracketIndex);
                }
            }

            ErrorObject errorToken;
            try {
                using var sr = new StringReader(jsonResponse);
                using var jsonErrorTextReader = new JsonTextReader(sr);
                errorToken = new JsonSerializer().Deserialize<ErrorObject>(jsonErrorTextReader);
            }
            catch (JsonException)
            {
                ModioLog.Error?.Log($"There is an error with the json response.");
                return new Error(ErrorCode.INVALID_JSON);
            }
            
            if (errorToken.Error.ErrorRef == 0)
            {
                ModioLog.Error?.Log($"Invalid error returned from API [{errorToken.Error.Code}], please contact mod.io support");
                return new Error(ErrorCode.UNKNOWN);
            }

            return new Error((ErrorCode)errorToken.Error.ErrorRef);
        }

        public Task<(Error error, T? result)> GetJson<T>(ModioAPIRequest request) where T : struct
        {
            return GetJson(request, reader => Task.FromResult((T?)new JsonSerializer().Deserialize<T>(reader)));
        }
        
        public Task<(Error error, JToken)> GetJson(ModioAPIRequest request)
        {
            return GetJson(request, reader => JToken.ReadFromAsync(reader));
        }


        private UploadHandler MapUploadHandler(ModioAPIRequest request)
        {
            switch (request.ContentType)
            {
                case ModioAPIRequestContentType.None: return null;

                case ModioAPIRequestContentType.FormUrlEncoded:
                    string formData = CreateFormUrlEncodedContent(request.Options.FormParameters);

                    if (!string.IsNullOrEmpty(formData))
                    {
                        return new UploadHandlerRaw(Encoding.UTF8.GetBytes(formData))
                        {
                            contentType = "application/x-www-form-urlencoded",
                        };
                    }

                    return null;

                case ModioAPIRequestContentType.MultipartFormData:
                    return CreateMultipartFormDataUploadHandler(request.Options);

                case ModioAPIRequestContentType.ByteArray: return PrepareByteArray(request.Options);

                case ModioAPIRequestContentType.String
                    when request.ContentTypeHint == "application/json":
                    return new UploadHandlerRaw(request.Options.BodyDataBytes)
                    {
                        contentType = request.ContentTypeHint,
                    };

                case ModioAPIRequestContentType.Multipart:
                case ModioAPIRequestContentType.Stream:

                default:
                    throw new NotImplementedException();
            }
        }

        private UploadHandler CreateMultipartFormDataUploadHandler(ModioAPIRequestOptions options)
        {
            string boundary = Guid.NewGuid().ToString().ToUpperInvariant();
            using var formData = new MemoryStream();

            using (var writer = new StreamWriter(formData, Encoding.UTF8, 1024, true)) // Keeping stream open
            {
                writer.WriteLine($"--{boundary}");

                foreach (KeyValuePair<string, string> formParameter in options.FormParameters)
                {
                    writer.WriteLine($"--{boundary}");
                    writer.WriteLine($"Content-Disposition: form-data; name={formParameter.Key}");
                    writer.WriteLine("Content-Type: text/plain; charset=utf-8");
                    writer.WriteLine(); // End of header
                    writer.WriteLine(formParameter.Value);
                }

                foreach (KeyValuePair<string, ModioAPIFileParameter> fileParameter in options.FileParameters)
                {
                    if (fileParameter.Value.Unused) continue;

                    using Stream stream = fileParameter.Value.GetContent();
                    if (stream == null) continue;

                    writer.WriteLine($"--{boundary}");

                    writer.WriteLine(
                        $"Content-Disposition: form-data; name=\"{fileParameter.Key}\"; filename=\"{fileParameter.Value.Name}\"; filename*=utf-8''{fileParameter.Value.Name}"
                    );

                    writer.WriteLine($"Content-Type: {fileParameter.Value.ContentType}");
                    writer.WriteLine(); // End of header
                    writer.Flush();     // Ensure header is written before the file content

                    stream.CopyTo(formData); // Directly copy the stream to avoid corruption

                    writer.WriteLine(); // Newline after file content
                }

                writer.WriteLine($"--{boundary}--"); // Final boundary marker
                writer.Flush();
            }

            byte[] bytes = formData.ToArray();

            var uploadHandler = new UploadHandlerRaw(bytes)
            {
                contentType = "multipart/form-data; boundary=" + boundary,
            };

            return uploadHandler;
        }

        static UploadHandler PrepareByteArray(ModioAPIRequestOptions options)
        {
            string boundary = Guid.NewGuid().ToString().ToUpperInvariant();

            return new UploadHandlerRaw(options.BodyDataBytes)
            {
                contentType = "multipart/form-data; boundary=" + boundary,
            };
        }

        private string CreateFormUrlEncodedContent(Dictionary<string, string> formParameters)
        {
            var sb = new StringBuilder();

            foreach (KeyValuePair<string, string> parameter in formParameters)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }

                sb.Append($"{UnityWebRequest.EscapeURL(parameter.Key)}={UnityWebRequest.EscapeURL(parameter.Value)}");
            }

            return sb.ToString();
        }

        private string MapMethod(ModioAPIRequestMethod method)
        {
            return method switch
            {
                ModioAPIRequestMethod.Post   => UnityWebRequest.kHttpVerbPOST,
                ModioAPIRequestMethod.Put    => UnityWebRequest.kHttpVerbPUT,
                ModioAPIRequestMethod.Get    => UnityWebRequest.kHttpVerbGET,
                ModioAPIRequestMethod.Delete => UnityWebRequest.kHttpVerbDELETE,
                _                            => throw new NotImplementedException(),
            };
        }

        private string BuildPath(ModioAPIRequest request)
        {
            var builder = new StringBuilder($"{_basePath}{request.GetUri(_defaultParameters)}");

            foreach (KeyValuePair<string, string> pathParameter in _pathParameters)
                builder.Replace($"{{{pathParameter.Key}}}", pathParameter.Value);

            return builder.ToString();
        }

        async Task<Error> SendRequest(UnityWebRequest webRequest, CancellationToken shutdownToken = default, CancellationToken token = default)
        {
            UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                if (token.IsCancellationRequested || shutdownToken.IsCancellationRequested)
                {
                    webRequest.Abort();
                    break;
                }
                await Task.Yield();
            }
            
            if (shutdownToken.IsCancellationRequested)
                return new Error(ErrorCode.SHUTTING_DOWN);

            if (token.IsCancellationRequested)
                return new Error(ErrorCode.OPERATION_CANCELLED);

            return Error.None;
        }

        public void Dispose()
        {
            foreach (UnityWebRequest webRequest in _webRequests)
            {
                webRequest?.Dispose();
            }
        }

        Task LogRequest(UnityWebRequest request, ModioAPIRequest modioRequest = null)
        {
            if (ModioLog.Verbose == null) return Task.CompletedTask;
            if (request == null) return Task.CompletedTask;

            var builder = new StringBuilder();
            builder.AppendLine($"{request.method} {request.uri.PathAndQuery} HTTP/1.1");

            if (modioRequest != null)
            {
                foreach ((string key, string value) in modioRequest.Options.HeaderParameters)
                {
                    builder.AppendLine(
                        string.Equals(key, "Authorization")
                            ? $"{key}: Bearer (omitted)"
                            : $"{key}: {string.Join(", ", value)}"
                        );
                }
            }
            
            foreach (var clientDefaultRequestHeader in _defaultParameters)
            {
                builder.AppendLine(clientDefaultRequestHeader);
            }
            
            if (request.uploadHandler != null && request.uploadHandler.data.Length != 0)
            {
                builder.AppendLine($"Content-Type: {request.uploadHandler.contentType}");
                builder.AppendLine();
                builder.Append(Encoding.UTF8.GetString(request.uploadHandler.data));
            }
            ModioLog.Verbose?.Log(builder.ToString());

            return Task.CompletedTask;
        }

        static bool IsResponseConnectionFailure(long responseCode)
            => responseCode == 0       // Generic can't reach server
               || responseCode == 408  // Request timeout
               || responseCode == 503; // Server unavailable;
    }
}
