using System.Collections.Generic;
using Modio.API.Interfaces;

namespace Modio
{
    public static class Version
    {
        static readonly System.Version Current = new System.Version(2026, 3, 0);
        static readonly List<string> EnvironmentDetails = new List<string>();

        public static void AddEnvironmentDetails(string details) => EnvironmentDetails.Add(details);
        
        public static string GetCurrent()
        {
            string output = $"modio.cs/{Current}";
            var envDetails = new List<string>(EnvironmentDetails);

            if (ModioServices.TryResolve(out IModioAPIInterface api))
            {
                envDetails.Add(api.GetType().Name);
            }

            if (envDetails.Count > 0)
                output = $"{output} ({string.Join("; ", envDetails)})";

            return output;
        }
    }
}
