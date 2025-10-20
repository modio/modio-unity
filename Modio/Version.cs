using System.Collections.Generic;

namespace Modio
{
    public static class Version
    {
        static readonly System.Version Current = new System.Version(2025, 10, 1);
        static readonly List<string> EnvironmentDetails = new List<string>();

        public static void AddEnvironmentDetails(string details) => EnvironmentDetails.Add(details);

        public static string GetCurrent()
        {
            string output = $"modio.cs/{Current}";
            var envDetails = new List<string>(EnvironmentDetails);

            if (ModioClient.Api is not null)
            {
                envDetails.Add($"{ModioClient.Api.GetType().Name}");
            }

            if (envDetails.Count > 0)
                output = string.Concat(output, $" ({string.Join("; ", envDetails)})");

            return output;
        }
    }
}
