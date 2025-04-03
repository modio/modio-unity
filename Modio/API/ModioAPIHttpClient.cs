using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Modio.API.Interfaces;
using Modio.API.SchemaDefinitions;
using Modio.Errors;
using Modio.Users;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Modio.API.HttpClient
{
    public class ModioAPIHttpClient : IModioAPIInterface
    {
        readonly System.Net.Http.HttpClient _client = new System.Net.Http.HttpClient();
        readonly List<string> _defaultParameters = new List<string>();
        readonly Dictionary<string, string> _pathParameters = new Dictionary<string, string>();
        string _basePath = string.Empty;
        
        CancellationTokenSource _cancellationTokenSource;

        Error CancelledOrShutDownError(CancellationToken shutdownCancellationToken) => new Error(shutdownCancellationToken.IsCancellationRequested ? ErrorCode.SHUTTING_DOWN : ErrorCode.OPERATION_CANCELLED);

        public void SetBasePath(string value) => _basePath = value;

        public void AddDefaultPathParameter(string key, string value) => _pathParameters.Add(key, value);

        public void RemoveDefaultPathParameter(string key) => _pathParameters.Remove(key);

        public void SetDefaultHeader(string name, string value)
        {
            RemoveDefaultHeader(name);
            _client.DefaultRequestHeaders.Add(name, value);
        }

        public void RemoveDefaultHeader(string name) => _client.DefaultRequestHeaders.Remove(name);

        public void AddDefaultParameter(string value) => _defaultParameters.Add(value);

        public void RemoveDefaultParameter(string value) => _defaultParameters.Remove(value);

        public void ResetConfiguration()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();
            
            _defaultParameters.Clear();
            _pathParameters.Clear();
            _basePath = string.Empty;
            _client.DefaultRequestHeaders.Clear();

            ModioClient.OnShutdown -= Shutdown;
            ModioClient.OnShutdown += Shutdown;
        }

        void Shutdown()
        {
            _cancellationTokenSource?.Cancel();
        }

        public async Task<(Error, Stream)> DownloadFile(string url, CancellationToken token = default)
        {
            Error error = await CheckFakeErrorsForTest(url);
            
            if(error)
                return (error, null);
            
            if (string.IsNullOrEmpty(url))
            {
                ModioLog.Error?.Log($"Attempting to download null url");
                return (new HttpError(HttpErrorCode.REQUEST_ERROR), null);
            }

            var downloadRequest = ModioAPIRequest.New(url);
            HttpMethod method = MapMethod(downloadRequest.Method);
            HttpContent content = MapContent(downloadRequest);
            string target = BuildPath(downloadRequest);

            var httpRequest = new HttpRequestMessage(method, target);
            httpRequest.Content = content;

            error = EnforceAuthentication(downloadRequest, httpRequest);
            if(error) return (error, null);

            await LogRequest(httpRequest);
            Stream stream;
            CancellationToken cachedShutdownToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

            // NOTE: Downloads aren't rate limited, so we don't check for them here
            try
            {
                if(token == default(CancellationToken))
                    token = cachedShutdownToken;
                    
                HttpResponseMessage response = await _client.SendAsync(
                    httpRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    token
                );

                stream = await response.Content.ReadAsStreamAsync();

                if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
                {
                    using var streamReader = new StreamReader(stream);
                    error = await GetErrorAndLogBadResponse(streamReader);
                    cachedShutdownToken.ThrowIfCancellationRequested();
                    return (error, null);
                }
                cachedShutdownToken.ThrowIfCancellationRequested();

                if (ModioAPI.IsOffline) ModioAPI.SetOfflineStatus(false);
            }
            catch (HttpRequestException e)
            {
                ModioLog.Error?.Log($"Can't reach mod.io servers for {target}: {e}");

                ModioAPI.SetOfflineStatus(true);

                return (new HttpError(HttpErrorCode.CANNOT_OPEN_CONNECTION), null);
            }
            catch (TaskCanceledException)
            {
                return (CancelledOrShutDownError(cachedShutdownToken), null);
            }
            catch (OperationCanceledException)
            {
                return (CancelledOrShutDownError(cachedShutdownToken), null);
            }
            catch (Exception e)
            {
                if (e.InnerException is TaskCanceledException) return (CancelledOrShutDownError(cachedShutdownToken), null);
                
                ModioLog.Error?.Log($"Exception in {target}: {e}");
                return (new ErrorException(e), null);
            }

            return (Error.None, stream);
        }

        static Error EnforceAuthentication(ModioAPIRequest downloadRequest, HttpRequestMessage httpRequest)
        {
            if (downloadRequest.Options.RequiresAuthentication && !User.Current.IsAuthenticated)
                return new Error(ErrorCode.USER_NOT_AUTHENTICATED);

            if(User.Current.IsAuthenticated)
                httpRequest.Headers.Add("Authorization", $"Bearer {User.Current?.GetAuthToken()}");

            return Error.None;
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

        HttpContent MapContent(ModioAPIRequest request)
        {
            if (request.Method == ModioAPIRequestMethod.Get)
            {
                return null;
            }

            switch (request.ContentType)
            {
                case ModioAPIRequestContentType.None:
                    var content = new StringContent(string.Empty);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    return content;

                case ModioAPIRequestContentType.FormUrlEncoded:
                    return new FormUrlEncodedContent(request.Options.FormParameters);

                case ModioAPIRequestContentType.MultipartFormData:
                    return PrepareMultipartFormDataContent(request.Options);

                case ModioAPIRequestContentType.ByteArray: return PrepareByteArray(request.Options);

                case ModioAPIRequestContentType.String
                    when request.ContentTypeHint == "application/json":
                    var byteArrayContent = new ByteArrayContent(request.Options.BodyDataBytes);
                    byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    return byteArrayContent;

                default: throw new NotImplementedException();
            }
        }

        HttpMethod MapMethod(ModioAPIRequestMethod method) => method switch
        {
            ModioAPIRequestMethod.Post   => HttpMethod.Post,
            ModioAPIRequestMethod.Put    => HttpMethod.Put,
            ModioAPIRequestMethod.Get    => HttpMethod.Get,
            ModioAPIRequestMethod.Delete => HttpMethod.Delete,
            _                            => throw new NotImplementedException(),
        };

        async Task<(Error error, T)> GetJson<T>(ModioAPIRequest request, Func<JsonTextReader, Task<T>> reader)
        {
            string target = BuildPath(request);

            Error error = await CheckFakeErrorsForTest(target);
            if(error)
                return (error, default(T));

            CancellationToken cachedShutdownToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

            try
            {
                HttpMethod method = MapMethod(request.Method);
                HttpContent content = MapContent(request);
                var httpRequest = new HttpRequestMessage(method, target);
                httpRequest.Content = content;

                error = EnforceAuthentication(request, httpRequest);
                if(error) return (error, default(T));

                foreach (KeyValuePair<string, string> headerParameter in request.Options.HeaderParameters)
                {
                    if (headerParameter.Key == "Content-Range")
                    {
                        if (httpRequest.Content != null)
                            httpRequest.Content.Headers.Add(headerParameter.Key, headerParameter.Value);
                    }
                    else
                        httpRequest.Headers.Add(headerParameter.Key, headerParameter.Value);
                }

                await LogRequest(httpRequest);
                HttpResponseMessage response = await _client.SendAsync(httpRequest, cachedShutdownToken);

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    cachedShutdownToken.ThrowIfCancellationRequested();
                    return (Error.None, (T)(object)new Response204()); //should always return a 204 object
                }

                ModioLog.Verbose?.Log(await response.Content.ReadAsStringAsync());

                await using Stream stream = await response.Content.ReadAsStreamAsync();
                using var streamReader = new StreamReader(stream);

                if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
                {
                    Error returnableError = await GetErrorAndLogBadResponse(streamReader);
                    
                    cachedShutdownToken.ThrowIfCancellationRequested();

                    if ((int)response.StatusCode == 429 &&
                        response.Headers.TryGetValues("retry-after", out IEnumerable<string> values) &&
                        int.TryParse(values.First(), out int retryAfterSeconds))
                    {
                        return (new RateLimitError(RateLimitErrorCode.RATELIMITED, retryAfterSeconds), default(T));
                    }

                    return (returnableError, default(T));
                }

                if (ModioAPI.IsOffline) ModioAPI.SetOfflineStatus(false);
                
                using var jsonTextReader = new JsonTextReader(streamReader);
                return (Error.None, await reader(jsonTextReader));
            }
            catch (HttpRequestException e)
            {
                ModioLog.Error?.Log($"Can't reach mod.io servers for {target}: {e}");
                
                ModioAPI.SetOfflineStatus(true);
                
                return (new HttpError(HttpErrorCode.CANNOT_OPEN_CONNECTION), default(T));
            }
            catch (TaskCanceledException) // From shutting down
            {
                return (CancelledOrShutDownError(cachedShutdownToken), default(T));
            }
            catch (OperationCanceledException)
            {
                return (CancelledOrShutDownError(cachedShutdownToken), default(T));
            }
            catch (Exception e)
            {                
                if (e.InnerException is TaskCanceledException or OperationCanceledException) return (CancelledOrShutDownError(cachedShutdownToken), default(T));

                ModioLog.Error?.Log($"Exception in {target}: {e}");
                return (new ErrorException(e), default(T));
            }
        }

        static async Task<Error> GetErrorAndLogBadResponse(StreamReader streamReader)
        {
            TextReader overrideStream = null;

            // It's possible (though hopefully rare) for the server to return a different error before our JSON ErrorObject
            if (streamReader.Peek() != '{')
            {
                string errorResponse = await streamReader.ReadToEndAsync();

                int firstOpenBracketIndex = errorResponse.IndexOf('{');

                if (firstOpenBracketIndex > 0)
                {
                    string serverError = errorResponse.Substring(0, firstOpenBracketIndex);
                    ModioLog.Error?.Log($"Unexpected error from server before JSON: {serverError}");
                    errorResponse = errorResponse.Substring(firstOpenBracketIndex);
                }
                else if (firstOpenBracketIndex == -1)
                {
                    if (errorResponse == "File Not Found") return new Error(ErrorCode.FILE_NOT_FOUND);

                    ModioLog.Error?.Log($"Unexpected error from server instead of JSON: {errorResponse}");
                    return new Error(ErrorCode.INVALID_JSON);
                }

                overrideStream = new StringReader(errorResponse);
            }

            ErrorObject errorToken;

            try
            {
                using var jsonErrorTextReader = new JsonTextReader(overrideStream ?? streamReader);
                errorToken = new JsonSerializer().Deserialize<ErrorObject>(jsonErrorTextReader);
            }
            catch (JsonException)
            {
                ModioLog.Error?.Log($"There is an error with the json response.");
                return new Error(ErrorCode.INVALID_JSON);
            }

            if (errorToken.Error.ErrorRef == 0)
            {
                ModioLog.Error?.Log("Invalid error returned from API, please contact mod.io support");
                return new Error(ErrorCode.UNKNOWN);
            }

            overrideStream?.Dispose();
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

        string BuildPath(ModioAPIRequest request)
        {
            string inputPath = request.GetUri(_defaultParameters);
            if (!inputPath.StartsWith("https://")) inputPath = $"{_basePath}{inputPath}";
            var builder = new StringBuilder(inputPath);

            foreach (KeyValuePair<string, string> pathParameter in _pathParameters)
                builder.Replace($"{{{pathParameter.Key}}}", pathParameter.Value);

            var target = builder.ToString();
            return target;
        }

        static HttpContent PrepareMultipartFormDataContent(ModioAPIRequestOptions options)
        {
            var multipartContent = new MultipartFormDataContent();

            foreach (KeyValuePair<string, string> formParameter in options.FormParameters)
            {
                multipartContent.Add(new StringContent(formParameter.Value), formParameter.Key);
            }

            foreach (KeyValuePair<string, ModioAPIFileParameter> fileParameter in options.FileParameters)
            {
                if (fileParameter.Value.Unused) continue;

                Stream stream = fileParameter.Value.GetContent();
                if (stream == null) continue;
                var streamContent = new StreamContent(stream);

                if (fileParameter.Value.ContentType != null)
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(fileParameter.Value.ContentType);

                if (fileParameter.Value.Name != null)
                    multipartContent.Add(streamContent, fileParameter.Key, fileParameter.Value.Name);
                else
                    multipartContent.Add(streamContent, fileParameter.Key);
            }

            return multipartContent;
        }

        static HttpContent PrepareByteArray(ModioAPIRequestOptions options)
        {
            var byteArrayContent = new ByteArrayContent(options.BodyDataBytes);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
            return byteArrayContent;
        }

        static async Task LogRequest(HttpRequestMessage request)
        {
            if (ModioLog.Verbose == null) return;
            if (request == null) return;

            var builder = new StringBuilder();
            builder.AppendLine($"{request.Method} {request.RequestUri} HTTP/{request.Version}");

            foreach (var header in request.Headers)
                builder.AppendLine(
                    string.Equals(header.Key, "Authorization")
                        ? $"{header.Key}: Bearer (omitted)"
                        : $"{header.Key}: {string.Join(", ", header.Value)}"
                );

            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                    builder.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");

                builder.AppendLine();
                builder.Append(await request.Content.ReadAsStringAsync());
            }

            ModioLog.Verbose.Log(builder.ToString());
        }

        public void Dispose() => _client.Dispose();
    }
}
