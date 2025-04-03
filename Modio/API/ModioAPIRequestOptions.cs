using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JsonConvert = Newtonsoft.Json.JsonConvert;   

[assembly: InternalsVisibleTo("Modio.UnityClient")]

namespace Modio.API
{
    public class ModioAPIRequestOptions : IDisposable
    {
        internal Dictionary<string, string> QueryParameters { get; } = new Dictionary<string, string>();
        internal Dictionary<string, string> HeaderParameters { get; } = new Dictionary<string, string>();
        internal bool RequiresAuthentication { get; private set; }

        internal Dictionary<string, string> FormParameters { get; } = new Dictionary<string, string>();

        internal Dictionary<string, ModioAPIFileParameter> FileParameters { get; } =
            new Dictionary<string, ModioAPIFileParameter>();

        public byte[] BodyDataBytes { get; private set; }

        public void Dispose()
        {
            RequiresAuthentication = false;
            HeaderParameters.Clear();
            QueryParameters.Clear();
            FormParameters.Clear();
            FileParameters.Clear();
        }

        internal void AddQueryParameter(string key, object value)
        {
            if (value != null) QueryParameters.Add(key, ParameterToString(value));
        }

        internal void AddHeaderParameter(string key, object value)
        {
            if (value != null) HeaderParameters.Add(key, ParameterToString(value));
        }

        internal void AddFilterParameters(SearchFilter filter)
        {
            if (filter == null) return;

            AddQueryParameter("_offset", filter.PageIndex * filter.PageSize);
            AddQueryParameter("_limit", filter.PageSize);

            foreach (KeyValuePair<string, object> parameter in filter.Parameters)
                AddQueryParameter(parameter.Key, parameter.Value);
        }

        public void RequireAuthentication() => RequiresAuthentication = true;

        static string ParameterToString(object value)
        {
            if (value == null) return string.Empty;
            if (value is IEnumerable<string> e) return string.Join(",", e);

            if (value is ICollection collection)
            {
                var stringBuilder = new StringBuilder();

                int index = 0;

                foreach (object o in collection)
                {
                    if (index++ > 0) stringBuilder.Append(",");
                    stringBuilder.Append(o);
                }

                return stringBuilder.ToString();
            }

            return $"{value}";
        }

        public void AddBody(byte[] data)
        {
            BodyDataBytes = data;
        }

        public void AddBody(IApiRequest request)
        {
            foreach (KeyValuePair<string, object> fileParam in request.GetBodyParameters())
            {
                if (fileParam.Value is ModioAPIFileParameter file)
                    FileParameters.Add(fileParam.Key, file);
                else
                    FormParameters.Add(fileParam.Key, ParameterToString(fileParam.Value));
            }
        }

        public void AddBody(IApiRequest request, string hint)
        {
            switch (hint)
            {
                case "application/json":
                    var nonNull = request.GetBodyParameters().Where(param => param.Value != null);
                    string body = JsonConvert.SerializeObject(nonNull);
                    AddBody(Encoding.UTF8.GetBytes(body));
                    break;

                default:
                    AddBody(request);
                    break;
            }
        }
    }
}
