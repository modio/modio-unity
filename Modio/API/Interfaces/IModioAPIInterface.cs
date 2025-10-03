using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Modio.Errors;
using Newtonsoft.Json.Linq;

namespace Modio.API.Interfaces
{
    public interface IModioAPIInterface : IDisposable
    {
        /// <summary>
        /// Sets the base path of the requests
        /// </summary>
        /// <param name="value">The base path</param>
        /// <see cref="ModioAPI"/>
        void SetBasePath(string value);
        
        /// <summary>
        /// Sets a default header value.
        /// </summary>
        /// <param name="name">The name of the header.</param>
        /// <param name="value">The value of the header.</param>
        void SetDefaultHeader(string name, string value);

        /// <summary>
        /// Sets a default path value
        /// </summary>
        /// <param name="key">The name of the path parameter</param>
        /// <param name="value">The value of the path parameter</param>
        void AddDefaultPathParameter(string key, string value);
        
        void RemoveDefaultPathParameter(string key);
        
        void RemoveDefaultHeader(string name);
        
        /// <summary>
        /// Adds a default query parameter to always be included with requests.
        /// </summary>
        /// <param name="value">The parameter to always be included in the form of "[key]=[value]"</param>
        void AddDefaultParameter(string value);
        
        void RemoveDefaultParameter(string value);
        
        /// <summary>
        /// Resets the configuration
        /// </summary>
        void ResetConfiguration();


        /// <summary>
        /// Downloads the file at url to the stream.
        /// </summary>
        /// <param name="url">The path to the file</param>
        /// <param name="token">The cancellation token</param>
        ///         /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="Stream"/> stream), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>stream</c> is the current stream of the download. <see cref="UserProfile"/>.</p>
        /// </returns>
        Task<(Error, Stream)> DownloadFile(string url, CancellationToken token = default, bool allowReauth = true);

        /// <summary>
        /// Requests a response from the mod.io API
        /// </summary>
        /// <param name="request">The request</param>
        /// <typeparam name="T">The expected return tye</typeparam>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="T"/>? result), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>result</c> is the object on success.</p>
        /// </returns>
        Task<(Error error, T? result)> GetJson<T>(ModioAPIRequest request, bool allowReauth = true) where T : struct;
        
        /// <summary>
        /// Requests a response from the mod.io API
        /// </summary>
        /// <param name="request">The request</param>
        /// <returns>
        /// <p>An asynchronous task that returns a tuple (<see cref="Error"/> error, <see cref="JToken"/>? result), where:</p>
        /// <p><c>error</c> is the error encountered during the task (if any)</p>
        /// <p><c>result</c> is the object.</p>
        /// </returns>
        Task<(Error error, JToken)>    GetJson(ModioAPIRequest request, bool allowReauth = true);
    }
}
