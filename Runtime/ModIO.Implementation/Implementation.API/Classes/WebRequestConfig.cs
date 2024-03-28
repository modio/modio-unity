using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ModIO.Implementation.API
{
    internal class WebRequestConfig<TResponse> : WebRequestConfig
    {
        public async Task<ResultAnd<TResponse>> RunViaWebRequestManager()
        {
            return await WebRequestManager.Request<TResponse>(this);
        }
    }

    internal class WebRequestConfig
    {
        public string Url;

        public string RequestMethodType;

        public bool ShouldRequestTimeout = true;

        /// <summary>
        /// If set to true, whether or not we have an available auth token, the WebRequestRunner
        /// will include the api key instead
        /// </summary>
        public bool DontUseAuthToken = false;

        public List<KeyValuePair<string,string>> StringKvpData = new List<KeyValuePair<string,string>>();

        public Stream DownloadStream;
        public List<BinaryDataContainer> BinaryData = new List<BinaryDataContainer>();
        public Dictionary<string, string> HeaderData = new Dictionary<string, string>();

        public bool HasBinaryData => BinaryData.Count > 0 || RawBinaryData?.Length > 0;
        public bool HasStringData => StringKvpData.Count > 0;

        public bool IsUpload => ForceIsUpload || HasBinaryData;

        public byte[] RawBinaryData { get; set; }

        public bool ForceIsUpload = false;

        public void AddField<TInput>(string key, TInput data)
        {
            if(data == null)
            {
                return;
            }

            StringKvpData.Add(new KeyValuePair<string,string>(key, data.ToString()));
        }

        public void AddField(string fieldName, string fileName, byte[] data)
            => BinaryData.Add(new BinaryDataContainer(fieldName, fileName, data));

        public void AddHeader(string key, string data) => HeaderData.Add(key, data);
    }
}
