namespace ModIO.Implementation.API.Requests
{
    internal static class Report
    {
        public static WebRequestConfig Request(ModIO.Report report)
        {
            var request = new WebRequestConfig()
            {
                Url =  $"{Settings.server.serverURL}{@"/report"}?",
                RequestMethodType = "POST"
            };

            request.AddField("id", report.id.ToString());
            request.AddField("resource", report.resourceType.ToString().ToLower());
            request.AddField("type", ((int)report.type).ToString());
            request.AddField("name", report.user);
            request.AddField("contact", report.contactEmail);
            request.AddField("summary", report.summary);

            return request;
        }
    }
}
