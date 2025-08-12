using System;
using System.Collections.Generic;
using System.Linq;

namespace Modio.API
{
    public class ModioAPIRequest : IDisposable
    {
        static readonly List<ModioAPIRequest> Pool = new List<ModioAPIRequest>();

        ModioAPIRequest() { }

        string Uri { get; set; }
        public ModioAPIRequestOptions Options { get; } = new ModioAPIRequestOptions();
        public ModioAPIRequestMethod Method { get; private set; } = ModioAPIRequestMethod.Get;
        public ModioAPIRequestContentType ContentType { get; private set; } = ModioAPIRequestContentType.None;
        
        public string ContentTypeHint { get; private set; } = "";

        internal static ModioAPIRequest New(
            string uri,
            ModioAPIRequestMethod method = ModioAPIRequestMethod.Get,
            ModioAPIRequestContentType contentType = ModioAPIRequestContentType.None,
            string contentTypeHint = ""
        )
        {
            ModioAPIRequest request;

            lock (Pool)
            {
                if (Pool.Count == 0)
                    request = new ModioAPIRequest();
                else
                {
                    int lastIndex = Pool.Count - 1;
                    request = Pool[lastIndex];
                    Pool.RemoveAt(lastIndex);
                }
            }

            request.Uri = uri;
            request.Method = method;
            request.ContentType = contentType;
            request.ContentTypeHint = contentTypeHint;

            request.Options.SetContentType(contentType);
            
            return request;
        }

        public string GetUri(List<string> defaultParameters)
        {
            var parameters = new string[defaultParameters.Count + Options.QueryParameters.Count];

            if (parameters.Length == 0) return Uri;
            Options.QueryParameters.Select(key => $"{key.Key}={key.Value}").ToArray().CopyTo(parameters, 0);
            defaultParameters.CopyTo(parameters, Options.QueryParameters.Count);

            return parameters.Length == 0
                ? Uri
                : $"{Uri}{(Uri.LastIndexOf('?') == -1 ? "?" : "&")}{string.Join("&", parameters)}";
        }

        public void Dispose()
        {
            Options.Dispose();
            Method = ModioAPIRequestMethod.Get;
            ContentType = ModioAPIRequestContentType.None;
            lock (Pool)
                Pool.Add(this);
        }
    }
}
